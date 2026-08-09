using Bunit;
using FluentValidation;
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
				_ => Task.FromResult<Outcome<NavigationResult>>(
					new Failed(Problem.ModelError(ErrorCategory.Conflict, "This email is already registered."))),
				_ => Task.FromResult<Outcome<NavigationResult>>(
					new Success<NavigationResult>(new() { NextUrl = "/Account/Login" })));
		Services.AddSingleton(service);
		Services.AddScoped<IValidator<RegisterRequest>>(_ =>
			new RegisterRequestValidator(service, NullLogger<RegisterRequestValidator>.Instance));

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

	// The blur/submit lock (spec §6.1, scoped down from the original four-part keystroke lock —
	// see the Task 12 fix report): the async email-exists rule fires on blur (the FluentTextInput's
	// change event) and at submit, and the cascade stops before the service call entirely once the
	// email fails its sync shape check first. A prior part 1 asserting "no call on keystroke input"
	// via the ontextimmediate event was removed — Register.razor's FluentTextInput never sets
	// Immediate="true" (it defaults to false), so that handler is FluentUI's own permanent no-op for
	// this page's markup regardless of whether the validation cascade is wired correctly, and the
	// assertion could never fail.
	[Fact]
	async Task The_email_exists_check_fires_on_blur_and_submit_and_stops_before_the_service_on_malformed_input()
	{
		var service = Substitute.For<IAuthenticationService>();
		service.EmailExists(Arg.Any<EmailExistsRequest>(), Arg.Any<CancellationToken>())
			.Returns(_ => Task.FromResult<Outcome<BoolResponse>>(new Success<BoolResponse>(new() { Value = false })));
		service.Register(Arg.Any<RegisterRequest>(), Arg.Any<CancellationToken>())
			.Returns(_ =>
				Task.FromResult<Outcome<NavigationResult>>(
					new Success<NavigationResult>(new() { NextUrl = "/Account/Login" })));
		Services.AddSingleton(service);
		Services.AddScoped<IValidator<RegisterRequest>>(_ =>
			new RegisterRequestValidator(service, NullLogger<RegisterRequestValidator>.Instance));

		var component = Render<Register>();
		var inputs = component.FindAll("fluent-text-input");

		// (1) change (blur): one call is permitted and expected
		await inputs[0].ChangeAsync("baw@example.com");
		await service.Received(1).EmailExists(Arg.Any<EmailExistsRequest>(), Arg.Any<CancellationToken>());

		// (2) sync-invalid submit (malformed email): cascade stops before the service
		service.ClearReceivedCalls();
		await inputs[0].ChangeAsync("not-an-email");
		await component.Find("form").SubmitAsync();
		await service.DidNotReceiveWithAnyArgs().EmailExists(default!, Xunit.TestContext.Current.CancellationToken);

		// (3) a second valid submit calls again
		service.ClearReceivedCalls();
		await inputs[0].ChangeAsync("baw@example.com");
		await inputs[1].ChangeAsync("correct horse battery");
		await component.Find("form").SubmitAsync();
		service.ReceivedCalls().Count(c => c.GetMethodInfo().Name == nameof(IAuthenticationService.EmailExists))
			.ShouldBeGreaterThanOrEqualTo(1);
	}
}
