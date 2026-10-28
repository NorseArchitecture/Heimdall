using Bunit;
using Bunit.TestDoubles;
using FluentValidation;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using Norse.Abstractions.Contracts;
using Norse.AuthN.Services;
using Norse.Primitives;

namespace Norse.AuthN.Components.FluentUI.Tests;

public sealed class LoginTests : InteractiveBunitContext
{
	readonly IAuthenticationService _loginService = Substitute.For<IAuthenticationService>();
	readonly ISessionTransition _sessionTransition = Substitute.For<ISessionTransition>();
	readonly ISignInMethodsService _signInMethods = Substitute.For<ISignInMethodsService>();

	public LoginTests()
	{
		Services.AddFluentUIComponents();
		// FluentUI components (FluentTextInput's password-visibility toggle, among others) make JS
		// interop calls bunit has no way to know about in advance — loose mode is bunit's own
		// documented answer, rather than hand-enumerating every internal call FluentUI might make.
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddSingleton(_sessionTransition);
		_signInMethods.GetSignInMethods(Arg.Any<GetSignInMethodsRequest>(), Arg.Any<CancellationToken>())
			.Returns(Outcome<SignInMethodsResponse>.Ok(new()));
		Services.AddSingleton(_signInMethods);
	}

	// Blazor matches query parameter names case-insensitively; both casings occur in the wild (the
	// scaffold's own redirects write ReturnUrl), so both must reach the request the server validates.
	[Theory]
	[InlineData("returnUrl")]
	[InlineData("ReturnUrl")]
	async Task Both_query_casings_reach_the_request(string name)
	{
		var service = SuccessfulLogin();
		Services.GetRequiredService<NavigationManager>().NavigateTo($"Account/Login?{name}=%2FAccount%2FManage");

		var component = Render<Login>();
		FillCredentials(component);
		await component.Find("form").SubmitAsync();

		await service.Received(1).Login(Arg.Is<LoginRequest>(r => r.ReturnUrl == "/Account/Manage"), Arg.Any<CancellationToken>());
	}

	[Fact]
	async Task No_return_url_sends_an_empty_string()
	{
		var service = SuccessfulLogin();

		var component = Render<Login>();
		FillCredentials(component);
		await component.Find("form").SubmitAsync();

		await service.Received(1).Login(Arg.Is<LoginRequest>(r => r.ReturnUrl == string.Empty), Arg.Any<CancellationToken>());
	}

	// Login is the password page, so it offers the passkey link and never a link back to itself.
	[Fact]
	void Login_hands_its_return_url_to_the_sign_in_links()
	{
		SuccessfulLogin();
		Services.GetRequiredService<NavigationManager>().NavigateTo("Account/Login?returnUrl=%2FAccount%2FManage");

		var component = Render<Login>();

		var links = component.FindComponent<SignInMethodLinks>().Instance;
		links.ReturnUrl.ShouldBe("/Account/Manage");
		links.ShowPasskey.ShouldBeTrue();
		links.ShowPassword.ShouldBeFalse();
		_signInMethods.Received(1).GetSignInMethods(Arg.Is<GetSignInMethodsRequest>(r => r.ReturnUrl == "/Account/Manage"), Arg.Any<CancellationToken>());
	}

	IAuthenticationService SuccessfulLogin()
	{
		var service = Substitute.For<IAuthenticationService>();
		service.Login(Arg.Any<LoginRequest>(), Arg.Any<CancellationToken>())
			.Returns(Outcome<NavigationResult>.Ok(new() { NextUrl = "/" }));
		Services.AddSingleton(service);
		return service;
	}

	[Fact]
	void WrongCredentials_CollapsedFailure_ShowsGenericMessage()
	{
		var service = Substitute.For<IAuthenticationService>();
		service.Login(Arg.Any<LoginRequest>(), Arg.Any<CancellationToken>())
			.Returns(_ =>
				Task.FromResult<Outcome<NavigationResult>>(
					new Failed(Problem.ModelError(ErrorCategory.InvalidCredentials, "Invalid email or password."))));
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
			.Returns(_ => Task.FromResult<Outcome<NavigationResult>>(new Failed(
				Problem.ModelError(ErrorCategory.LockedOut, "Your account is locked. Try again in 15 minutes."))));
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
				_ => Task.FromResult<Outcome<NavigationResult>>(
					new Failed(Problem.ModelError(ErrorCategory.InvalidCredentials, "Invalid email or password."))),
				_ => Task.FromResult<Outcome<NavigationResult>>(
					new Success<NavigationResult>(new() { NextUrl = "/" })));
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

	// A correct password with 2FA pending rides the SUCCESS side of the Outcome (the credential check
	// genuinely passed) but must never be treated as a completed login. The server resolves NextUrl to
	// the full navigation target itself (route, RememberMe query string included) -- the component
	// neither branches on a flag nor constructs a route nor supplies a default; it just navigates to
	// whatever URL came back.
	[Fact]
	void NextUrl_RoutesToTheTwoFactorChallenge_NotToACompletedLogin()
	{
		var service = Substitute.For<IAuthenticationService>();
		service.Login(Arg.Any<LoginRequest>(), Arg.Any<CancellationToken>())
			.Returns(_ =>
				Task.FromResult<Outcome<NavigationResult>>(
					new Success<NavigationResult>(new() { NextUrl = "Account/LoginWith2fa?RememberMe=false" })));
		Services.AddSingleton(service);
		var navigation = Services.GetRequiredService<BunitNavigationManager>();

		var component = Render<Login>();
		FillCredentials(component);
		component.Find("form").Submit();

		_sessionTransition.Received(1).Begin(new() { NextUrl = "Account/LoginWith2fa?RememberMe=false" });
		Services.GetRequiredService<BunitNavigationManager>().History.ShouldBeEmpty();
	}

	// SubmitAsync gates on FormValidator's pass, so nothing dispatches until Email/Password satisfy
	// LoginRequestValidator's rules (NotEmpty+EmailAddress, NotEmpty+MinimumLength(8)) — both tests
	// need real, valid-format input before submitting, or the mocked service is never reached at all.
	static void FillCredentials(IRenderedComponent<Login> component)
	{
		var inputs = component.FindAll("fluent-text-input");
		inputs[0].Change("user@example.com");
		inputs[1].Change("Password123");
	}

	[Fact]
	async Task An_empty_submit_renders_the_email_required_message()
	{
		var component = RenderWithValidator();

		await component.Find("form").SubmitAsync();

		component.Markup.ShouldContain("Enter your email address.");
		await _loginService.DidNotReceiveWithAnyArgs().Login(default!, Xunit.TestContext.Current.CancellationToken);
	}

	[Fact]
	async Task A_malformed_email_renders_the_format_message()
	{
		var component = RenderWithValidator();

		await component.FindAll("fluent-text-input")[0].ChangeAsync("not-an-email");
		await component.Find("form").SubmitAsync();

		component.Markup.ShouldContain("Enter a valid email address (local@domain.tld).");
		await _loginService.DidNotReceiveWithAnyArgs().Login(default!, Xunit.TestContext.Current.CancellationToken);
	}

	IRenderedComponent<Login> RenderWithValidator()
	{
		Services.AddSingleton(_loginService);
		Services.AddScoped<IValidator<LoginRequest>, LoginRequestValidator>();
		return Render<Login>();
	}
}
