using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using Norse.AuthN.Services;
using Norse.Abstractions.Contracts;

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
		var gateway = Substitute.For<IAuthenticationGateway>();
		gateway.Login(Arg.Any<LoginRequest>(), Arg.Any<CancellationToken>())
			.Returns(_ => ValueTask.FromResult(Outcome<LoginResult>.Ok(new LoginResult { Succeeded = false })));
		Services.AddSingleton(gateway);

		var component = Render<Login>();
		FillCredentials(component);
		component.Find("form").Submit();

		component.Markup.ShouldContain("Invalid email or password.");
	}

	[Fact]
	void LockedOut_RealFailure_ShowsDistinguishableMessage()
	{
		var gateway = Substitute.For<IAuthenticationGateway>();
		gateway.Login(Arg.Any<LoginRequest>(), Arg.Any<CancellationToken>())
			.Returns(_ => ValueTask.FromResult(Outcome<LoginResult>.Err(ErrorCategory.LockedOut,
				new Dictionary<string, string[]> { [""] = ["Your account is locked. Try again in 15 minutes."] })));
		Services.AddSingleton(gateway);

		var component = Render<Login>();
		FillCredentials(component);
		component.Find("form").Submit();

		component.Markup.ShouldContain("Your account is locked. Try again in 15 minutes.");
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
