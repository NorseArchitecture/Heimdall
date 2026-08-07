using Bunit;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using Norse.AuthN.Services;
using Norse.Abstractions.Contracts;
using Norse.Primitives;

namespace Norse.AuthN.Components.FluentUI.Tests;

public sealed class LoginTests : BunitContext
{
	public LoginTests()
	{
		Services.AddFluentUIComponents();
		// FluentUI components (FluentTextInput's password-visibility toggle, among others) make JS
		// interop calls bunit has no way to know about in advance — loose mode is bunit's own
		// documented answer, rather than hand-enumerating every internal call FluentUI might make.
		JSInterop.Mode = JSRuntimeMode.Loose;
	}

	[Fact]
	void WrongCredentials_CollapsedFailure_ShowsGenericMessage()
	{
		var service = Substitute.For<IAuthenticationService>();
		service.Login(Arg.Any<LoginRequest>(), Arg.Any<CancellationToken>())
			.Returns(_ => Task.FromResult<Outcome<LoginResult>>(new Failed(Problem.ModelError(ErrorCategory.InvalidCredentials, "Invalid email or password."))));
		Services.AddSingleton(service);

		var component = Render<Login>();
		FillCredentials(component);
		component.Find("form").Submit();

		component.Markup.ShouldContain("Invalid email or password.");
	}

	[Fact]
	void LockedOut_RealFailure_ShowsDistinguishableMessage()
	{
		var service = Substitute.For<IAuthenticationService>();
		service.Login(Arg.Any<LoginRequest>(), Arg.Any<CancellationToken>())
			.Returns(_ => Task.FromResult<Outcome<LoginResult>>(new Failed(Problem.ModelError(ErrorCategory.LockedOut, "Your account is locked. Try again in 15 minutes."))));
		Services.AddSingleton(service);

		var component = Render<Login>();
		FillCredentials(component);
		component.Find("form").Submit();

		component.Markup.ShouldContain("Your account is locked. Try again in 15 minutes.");
	}

	// The deadlock lock (spec §10): a rejected submit must be correctable — the server-error store
	// has to yield to the next validation pass, or the resubmit never dispatches at all. Fails against
	// the pre-Task-12 hand-rolled EditContext/ValidationMessageStore plumbing (only 1 of 2 expected
	// Login calls land — proven RED before ApplyServerErrors/ServerErrorCoordinator existed).
	[Fact]
	async Task A_rejected_login_can_be_corrected_and_resubmitted()
	{
		var service = Substitute.For<IAuthenticationService>();
		service.Login(Arg.Any<LoginRequest>(), Arg.Any<CancellationToken>())
			.Returns(
				_ => Task.FromResult<Outcome<LoginResult>>(new Failed(Problem.ModelError(ErrorCategory.InvalidCredentials, "Invalid email or password."))),
				_ => Task.FromResult<Outcome<LoginResult>>(new Success<LoginResult>(new() { DeferredCompletionUrl = "/" })));
		Services.AddSingleton(service);
		Services.AddScoped<IValidator<LoginRequest>, LoginRequestValidator>();

		var component = Render<Login>();
		var inputs = component.FindAll("fluent-text-input");

		// (1) server rejection applied
		await inputs[0].ChangeAsync("baw@example.com");
		await inputs[1].ChangeAsync("wrong-password-1");
		await component.Find("form").SubmitAsync();
		component.Markup.ShouldContain("Invalid email or password.");

		// (2) user edits a field — (3) client validation passes — (4) second server call dispatches
		await inputs[1].ChangeAsync("right-password-11");
		await component.Find("form").SubmitAsync();

		await service.Received(2).Login(Arg.Any<LoginRequest>(), Arg.Any<CancellationToken>());
	}

	// FluentValidator blocks OnValidSubmit until Email/Password pass LoginRequestValidator's rules
	// (NotEmpty+EmailAddress, NotEmpty+MinimumLength(8)) — both tests need real, valid-format input
	// before submitting, or the mocked gateway is never reached at all.
	static void FillCredentials(IRenderedComponent<Login> component)
	{
		var inputs = component.FindAll("fluent-text-input");
		inputs[0].Change("user@example.com");
		inputs[1].Change("Password123");
	}
}
