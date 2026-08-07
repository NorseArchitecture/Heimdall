using Bunit;
using FluentValidation;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.FluentUI.AspNetCore.Components;
using Norse.AuthN.Services;
using Norse.Abstractions.Contracts;
using Norse.Primitives;

namespace Norse.AuthN.Components.FluentUI.Tests;

public sealed class RegisterTests : BunitContext
{
	public RegisterTests()
	{
		Services.AddFluentUIComponents();
		// FluentUI components make JS interop calls bunit has no way to know about in advance —
		// loose mode is bunit's own documented answer (same rationale as LoginTests).
		JSInterop.Mode = JSRuntimeMode.Loose;
	}

	// The deadlock lock (spec §10), model-level rejection variant — a race-lost registration
	// (Conflict, discovered only at the real Register() call, past the pre-submit EmailExists check)
	// must be correctable the same way a rejected login is. Fails against the pre-Task-12 hand-rolled
	// EditContext/ValidationMessageStore plumbing for the identical reason Login's does.
	[Fact]
	async Task A_rejected_registration_can_be_corrected_and_resubmitted()
	{
		var service = Substitute.For<IAuthenticationService>();
		service.EmailExists(Arg.Any<EmailExistsRequest>(), Arg.Any<CancellationToken>())
			.Returns(_ => Task.FromResult<Outcome<BoolResponse>>(new Success<BoolResponse>(new() { Value = false })));
		service.Register(Arg.Any<RegisterRequest>(), Arg.Any<CancellationToken>())
			.Returns(
				_ => Task.FromResult<Outcome<RegisterResult>>(new Failed(Problem.ModelError(ErrorCategory.Conflict, "This email is already registered."))),
				_ => Task.FromResult<Outcome<RegisterResult>>(new Success<RegisterResult>(new() { Succeeded = true })));
		Services.AddSingleton(service);
		Services.AddScoped<IValidator<RegisterRequest>>(_ => new RegisterRequestValidator(service, NullLogger<RegisterRequestValidator>.Instance));

		var component = Render<Register>();
		var inputs = component.FindAll("fluent-text-input");

		// (1) server rejection applied
		await inputs[0].ChangeAsync("baw@example.com");
		await inputs[1].ChangeAsync("correct horse battery");
		await component.Find("form").SubmitAsync();
		component.Markup.ShouldContain("This email is already registered.");

		// (2) user edits a field — (3) client validation passes — (4) second server call dispatches
		await inputs[1].ChangeAsync("correct horse battery 2");
		await component.Find("form").SubmitAsync();

		await service.Received(2).Register(Arg.Any<RegisterRequest>(), Arg.Any<CancellationToken>());
	}

	// The blur-semantics lock (spec §6.1): the async email-exists rule must fire on blur (the
	// FluentTextInput's change event) and at submit, but never per keystroke — Blazilla's field-change
	// pass builds a member-name selector, so an @oninput-only edit never reaches the model-level
	// validation pass that would run the email rule at all.
	[Fact]
	async Task The_email_exists_check_fires_on_blur_and_submit_but_never_per_keystroke()
	{
		var service = Substitute.For<IAuthenticationService>();
		service.EmailExists(Arg.Any<EmailExistsRequest>(), Arg.Any<CancellationToken>())
			.Returns(_ => Task.FromResult<Outcome<BoolResponse>>(new Success<BoolResponse>(new() { Value = false })));
		service.Register(Arg.Any<RegisterRequest>(), Arg.Any<CancellationToken>())
			.Returns(_ => Task.FromResult<Outcome<RegisterResult>>(new Success<RegisterResult>(new() { Succeeded = true })));
		Services.AddSingleton(service);
		Services.AddScoped<IValidator<RegisterRequest>>(_ => new RegisterRequestValidator(service, NullLogger<RegisterRequestValidator>.Instance));

		var component = Render<Register>();
		var inputs = component.FindAll("fluent-text-input");

		// (1) keystroke input without a change event: no call — FluentTextInput's native web
		// component raises 'ontextimmediate' per keystroke, not the standard HTML 'oninput'.
		await inputs[0].TriggerEventAsync("ontextimmediate", new ChangeEventArgs { Value = "baw@example.com" });
		await service.DidNotReceiveWithAnyArgs().EmailExists(default!, Xunit.TestContext.Current.CancellationToken);

		// (2) change (blur): one call is permitted and expected
		await inputs[0].ChangeAsync("baw@example.com");
		await service.Received(1).EmailExists(Arg.Any<EmailExistsRequest>(), Arg.Any<CancellationToken>());

		// (3) sync-invalid submit (malformed email): cascade stops before the service
		service.ClearReceivedCalls();
		await inputs[0].ChangeAsync("not-an-email");
		await component.Find("form").SubmitAsync();
		await service.DidNotReceiveWithAnyArgs().EmailExists(default!, Xunit.TestContext.Current.CancellationToken);

		// (4) a second valid submit calls again
		service.ClearReceivedCalls();
		await inputs[0].ChangeAsync("baw@example.com");
		await inputs[1].ChangeAsync("correct horse battery");
		await component.Find("form").SubmitAsync();
		service.ReceivedCalls().Count(c => c.GetMethodInfo().Name == nameof(IAuthenticationService.EmailExists))
			.ShouldBeGreaterThanOrEqualTo(1);
	}
}
