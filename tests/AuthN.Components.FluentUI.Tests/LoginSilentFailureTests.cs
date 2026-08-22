using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using Norse.Abstractions.Contracts;
using Norse.AuthN.Services;

namespace Norse.AuthN.Components.FluentUI.Tests;

/// <summary>
///     What a silent 401/InvalidCredentials decodes to on the client: category only, zero
///     <see cref="Problem.Errors" />. Login renders a fixed local message for both instead of the
///     ServerValidation bridge's per-category sentence, which used to differ between the two.
/// </summary>
public sealed class LoginSilentFailureTests
{
	[Fact]
	void Renders_a_local_message_when_the_server_sends_a_bodyless_unauthorized()
	{
		// What a silent 401 decodes to on the client: category only, zero errors.
		var outcome = Outcome<NavigationResult>.Err(ErrorCategory.Unauthorized);

		RenderLoginWith(outcome).ShouldContain("Invalid email or password.");
	}

	[Fact]
	void The_local_message_is_identical_for_every_silent_category()
	{
		var unauthorized = RenderLoginWith(Outcome<NavigationResult>.Err(ErrorCategory.Unauthorized));
		var invalid = RenderLoginWith(Outcome<NavigationResult>.Err(ErrorCategory.InvalidCredentials));

		invalid.ShouldBe(unauthorized);
	}

	// A fresh BunitContext per render -- bunit's Services provider locks once a component has resolved
	// from it, so comparing two renders (the second fact above) needs two independent containers, not
	// one reused across two Render<Login>() calls. SubmitAsync gates on FormValidator's pass, so nothing
	// dispatches until Email/Password satisfy LoginRequestValidator's rules -- valid-format input is
	// required before submitting, or the mocked service is never reached at all (mirrors
	// LoginTests.cs's FillCredentials). Returns only ModelValidationSummary's rendered text, not the
	// full page markup: FluentUI's input/label elements each carry a fresh per-instance id attribute
	// (fluent-field/fluent-text-input), so two independent renders never produce byte-identical markup
	// even when they display the identical message -- the id churn is incidental to this component's own
	// rendering, not a difference the "identical message" fact is about.
	static string RenderLoginWith(Outcome<NavigationResult> outcome)
	{
		using BunitContext context = new();
		// FluentUI components (FluentTextInput's password-visibility toggle, among others) make JS
		// interop calls bunit has no way to know about in advance — loose mode is bunit's own
		// documented answer, rather than hand-enumerating every internal call FluentUI might make.
		context.Services.AddFluentUIComponents();
		context.JSInterop.Mode = JSRuntimeMode.Loose;
		context.Services.AddSingleton(Substitute.For<ISessionTransition>());

		var service = Substitute.For<IAuthenticationService>();
		service.Login(Arg.Any<LoginRequest>(), Arg.Any<CancellationToken>())
			.Returns(_ => Task.FromResult(outcome));
		context.Services.AddSingleton(service);

		var component = context.Render<Login>();
		var inputs = component.FindAll("fluent-text-input");
		inputs[0].Change("user@example.com");
		inputs[1].Change("Password123");
		component.Find("form").Submit();

		return component.Find(".norse-model-errors").TextContent.Trim();
	}
}
