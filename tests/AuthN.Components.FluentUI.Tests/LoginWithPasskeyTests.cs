using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;
using Norse.Abstractions.Contracts;
using Norse.AuthN.Services;

namespace Norse.AuthN.Components.FluentUI.Tests;

// passkeys.js is pinned through bUnit's module interop here and by the walk's virtual authenticator
// (T3-25), as in PasskeysTests: Node cannot exercise WebAuthn, so there is no JS harness.
public sealed class LoginWithPasskeyTests : InteractiveBunitContext
{
	const string OptionsJson = """{"challenge":"c"}""";
	const string CredentialJson = """{"id":"cred"}""";
	const string Dismissed = "No passkey was provided by the authenticator.";
	const string InvalidPasskey = "The passkey could not be used to sign in.";

	readonly ISignInMethodsService _signInMethods = Substitute.For<ISignInMethodsService>();
	readonly ISessionTransition _sessionTransition = Substitute.For<ISessionTransition>();

	public LoginWithPasskeyTests()
	{
		Services.AddFluentUIComponents();
		// FluentUI components make JS interop calls bunit has no way to know about in advance —
		// loose mode is bunit's own documented answer (same rationale as LoginTests).
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddSingleton(_signInMethods).AddSingleton(_sessionTransition);
		_signInMethods.GetSignInMethods(Arg.Any<GetSignInMethodsRequest>(), Arg.Any<CancellationToken>())
			.Returns(Outcome<SignInMethodsResponse>.Ok(new()));
		_signInMethods.GetPasskeyRequestOptions(Arg.Any<CancellationToken>())
			.Returns(Outcome<PasskeyRequestOptionsResponse>.Ok(new() { OptionsJson = OptionsJson, AssertionState = "state" }));
	}

	BunitNavigationManager Navigation => Services.GetRequiredService<BunitNavigationManager>();

	BunitJSModuleInterop Module(bool supported = true)
	{
		var module = JSInterop.SetupModule(PasskeyModule.Path);
		module.Setup<bool>("isSupported").SetResult(supported);
		return module;
	}

	static void Ceremony(BunitJSModuleInterop module, string? credentialJson, string? error) =>
		module.Setup<PasskeyCeremonyResult>("request", OptionsJson, "optional").SetResult(new(credentialJson, error));

	[Fact]
	void A_bare_render_performs_nothing()
	{
		Module();

		var page = Render<LoginWithPasskey>();

		page.Find("h1").TextContent.ShouldBe("Log in with a passkey");
		page.Find("fluent-button[data-action='passkey']").HasAttribute("disabled").ShouldBeFalse();
		_signInMethods.DidNotReceiveWithAnyArgs().GetPasskeyRequestOptions(Xunit.TestContext.Current.CancellationToken);
		_signInMethods.DidNotReceiveWithAnyArgs().PasskeySignIn(null!, Xunit.TestContext.Current.CancellationToken);
		_sessionTransition.DidNotReceiveWithAnyArgs().Begin(null!);
	}

	// Blazor matches query names case-insensitively, and the scaffold's own redirects write ReturnUrl,
	// so both casings must reach the request the server validates.
	[Theory]
	[InlineData("returnUrl")]
	[InlineData("ReturnUrl")]
	async Task A_completed_assertion_signs_in_and_begins_one_transition(string name)
	{
		var module = Module();
		Ceremony(module, CredentialJson, null);
		_signInMethods.PasskeySignIn(Arg.Any<PasskeySignInRequest>(), Arg.Any<CancellationToken>())
			.Returns(Outcome<NavigationResult>.Ok(new() { NextUrl = "/Account/Manage" }));
		Navigation.NavigateTo($"Account/LoginWithPasskey?{name}=%2FAccount%2FManage");
		var page = Render<LoginWithPasskey>();

		await page.Find("fluent-checkbox").ChangeAsync(true);
		await page.Find("fluent-button[data-action='passkey']").ClickAsync(new());

		await _signInMethods.Received(1).PasskeySignIn(
			Arg.Is<PasskeySignInRequest>(r => r.CredentialJson == CredentialJson && r.AssertionState == "state" && r.RememberMe && r.ReturnUrl == "/Account/Manage"),
			Arg.Any<CancellationToken>());
		_sessionTransition.Received(1).Begin(new() { NextUrl = "/Account/Manage" });
		// The seam owns the navigation; the page never navigates on its own (the one history entry is the arrange's).
		Navigation.History.ShouldHaveSingleItem();
	}

	[Fact]
	async Task No_return_url_sends_an_empty_string()
	{
		var module = Module();
		Ceremony(module, CredentialJson, null);
		_signInMethods.PasskeySignIn(Arg.Any<PasskeySignInRequest>(), Arg.Any<CancellationToken>())
			.Returns(Outcome<NavigationResult>.Ok(new() { NextUrl = "/" }));
		var page = Render<LoginWithPasskey>();

		await page.Find("fluent-button[data-action='passkey']").ClickAsync(new());

		await _signInMethods.Received(1).PasskeySignIn(
			Arg.Is<PasskeySignInRequest>(r => r.ReturnUrl == string.Empty && !r.RememberMe),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	void A_dismissed_prompt_shows_the_message_and_calls_nothing()
	{
		var module = Module();
		Ceremony(module, null, Dismissed);
		var page = Render<LoginWithPasskey>();

		page.Find("fluent-button[data-action='passkey']").Click();

		page.Find(".alert-danger").TextContent.Trim().ShouldBe(Dismissed);
		_signInMethods.DidNotReceiveWithAnyArgs().PasskeySignIn(null!, Xunit.TestContext.Current.CancellationToken);
		_sessionTransition.DidNotReceiveWithAnyArgs().Begin(null!);
	}

	[Fact]
	void An_aborted_ceremony_calls_nothing_and_shows_nothing()
	{
		var module = Module();
		Ceremony(module, null, null);
		var page = Render<LoginWithPasskey>();

		page.Find("fluent-button[data-action='passkey']").Click();

		page.FindAll(".alert").ShouldBeEmpty();
		_signInMethods.DidNotReceiveWithAnyArgs().PasskeySignIn(null!, Xunit.TestContext.Current.CancellationToken);
		page.Find("fluent-button[data-action='passkey']").HasAttribute("disabled").ShouldBeFalse();
	}

	// InvalidCredentials covers a forged or replayed assertion and a missing UV flag alike; the page
	// authors the one line, whatever the server sent, so nothing distinguishes the three.
	[Fact]
	void A_failed_sign_in_renders_in_place()
	{
		var module = Module();
		Ceremony(module, CredentialJson, null);
		_signInMethods.PasskeySignIn(Arg.Any<PasskeySignInRequest>(), Arg.Any<CancellationToken>())
			.Returns(new Outcome<NavigationResult>(new Failed(Problem.ModelError(ErrorCategory.InvalidCredentials, "Server text."))));
		var page = Render<LoginWithPasskey>();

		page.Find("fluent-button[data-action='passkey']").Click();

		page.Find(".alert-danger").TextContent.Trim().ShouldBe(InvalidPasskey);
		_sessionTransition.DidNotReceiveWithAnyArgs().Begin(null!);
		page.Find("fluent-button[data-action='passkey']").HasAttribute("disabled").ShouldBeFalse();
	}

	[Fact]
	void A_failure_the_server_explains_renders_its_text()
	{
		const string Locked = "Your account is locked. Try again in 15 minutes.";
		var module = Module();
		Ceremony(module, CredentialJson, null);
		_signInMethods.PasskeySignIn(Arg.Any<PasskeySignInRequest>(), Arg.Any<CancellationToken>())
			.Returns(new Outcome<NavigationResult>(new Failed(Problem.ModelError(ErrorCategory.LockedOut, Locked))));
		var page = Render<LoginWithPasskey>();

		page.Find("fluent-button[data-action='passkey']").Click();

		page.Find(".alert-danger").TextContent.Trim().ShouldBe(Locked);
	}

	[Fact]
	void A_failed_options_load_renders_in_place_and_runs_no_ceremony()
	{
		var module = Module();
		_signInMethods.GetPasskeyRequestOptions(Arg.Any<CancellationToken>())
			.Returns(Outcome<PasskeyRequestOptionsResponse>.Err(ErrorCategory.Fault, correlationId: Guid.Empty));
		var page = Render<LoginWithPasskey>();

		page.Find("fluent-button[data-action='passkey']").Click();

		page.Find(".alert-danger").TextContent.Trim().ShouldBe("Passkey sign-in failed. Please try again.");
		module.Invocations.ShouldNotContain(invocation => invocation.Identifier == "request");
	}

	[Fact]
	void A_js_exception_renders_in_place()
	{
		var module = Module();
		module.Setup<PasskeyCeremonyResult>("request", OptionsJson, "optional").SetException(new JSException("The operation is insecure."));
		var page = Render<LoginWithPasskey>();

		page.Find("fluent-button[data-action='passkey']").Click();

		page.Find(".alert-danger").TextContent.Trim().ShouldBe("The operation is insecure.");
		_signInMethods.DidNotReceiveWithAnyArgs().PasskeySignIn(null!, Xunit.TestContext.Current.CancellationToken);
	}

	[Fact]
	void An_unsupported_browser_hides_the_button()
	{
		Module(supported: false);

		var page = Render<LoginWithPasskey>();

		page.FindAll("fluent-button[data-action='passkey']").ShouldBeEmpty();
		page.Markup.ShouldContain("Some passkey features are missing. Please update your browser.");
	}

	[Fact]
	void A_failed_module_import_renders_in_place_and_keeps_the_button_disabled()
	{
		Services.AddSingleton<IJSRuntime>(new FailingImportRuntime(JSInterop.JSRuntime, PasskeyModule.Path));

		var page = Render<LoginWithPasskey>();

		page.Find(".alert-danger").TextContent.Trim().ShouldBe("Failed to fetch dynamically imported module.");
		page.Find("fluent-button[data-action='passkey']").HasAttribute("disabled").ShouldBeTrue();
	}

	// The lock row alone cannot fail (a static render never learns support, so the button is disabled either
	// way); this pins both guards on a live circuit: CanAct while the options dispatch is in flight, and _busy
	// across the ceremony, where the base's in-flight guard is released.
	[Fact]
	async Task The_button_is_disabled_while_the_ceremony_runs()
	{
		var module = Module();
		var ceremony = module.Setup<PasskeyCeremonyResult>("request", OptionsJson, "optional");
		TaskCompletionSource<Outcome<PasskeyRequestOptionsResponse>> options = new();
		_signInMethods.GetPasskeyRequestOptions(Arg.Any<CancellationToken>()).Returns(options.Task);
		var page = Render<LoginWithPasskey>();

		var click = page.Find("fluent-button[data-action='passkey']").ClickAsync(new());

		page.Find("fluent-button[data-action='passkey']").HasAttribute("disabled").ShouldBeTrue("options in flight");

		await page.InvokeAsync(() => options.SetResult(
			Outcome<PasskeyRequestOptionsResponse>.Ok(new() { OptionsJson = OptionsJson, AssertionState = "state" })));
		page.WaitForAssertion(() => module.Invocations.ShouldContain(invocation => invocation.Identifier == "request"));

		page.Find("fluent-button[data-action='passkey']").HasAttribute("disabled").ShouldBeTrue("ceremony in flight");

		ceremony.SetResult(new(null, null));
		await click;
		page.Find("fluent-button[data-action='passkey']").HasAttribute("disabled").ShouldBeFalse();
	}

	// A browser that answers after the page is gone: nothing is sent on its behalf.
	[Fact]
	async Task A_page_disposed_mid_ceremony_calls_nothing_and_throws_nothing()
	{
		var module = Module();
		var ceremony = module.Setup<PasskeyCeremonyResult>("request", OptionsJson, "optional");
		var page = Render<LoginWithPasskey>();
		var click = page.Find("fluent-button[data-action='passkey']").ClickAsync(new());
		page.WaitForAssertion(() => module.Invocations.ShouldContain(invocation => invocation.Identifier == "request"));

		await page.InvokeAsync(async () => await page.Instance.DisposeAsync());
		ceremony.SetResult(new(CredentialJson, null));

		await Should.NotThrowAsync(click);
		_ = _signInMethods.DidNotReceiveWithAnyArgs().PasskeySignIn(null!, Xunit.TestContext.Current.CancellationToken);
		_sessionTransition.DidNotReceiveWithAnyArgs().Begin(null!);
	}

	// The passkey page offers the password link and never a link back to itself; every URL is the server's.
	[Fact]
	void The_page_hands_its_return_url_to_the_sign_in_links()
	{
		Module();
		Navigation.NavigateTo("Account/LoginWithPasskey?returnUrl=%2FAccount%2FManage");

		var page = Render<LoginWithPasskey>();

		var links = page.FindComponent<SignInMethodLinks>().Instance;
		links.ReturnUrl.ShouldBe("/Account/Manage");
		links.ShowPassword.ShouldBeTrue();
		links.ShowPasskey.ShouldBeFalse();
		_signInMethods.Received(1).GetSignInMethods(Arg.Is<GetSignInMethodsRequest>(r => r.ReturnUrl == "/Account/Manage"), Arg.Any<CancellationToken>());
	}

	// Fails the import of one module path, as a missing asset or a CSP block would; delegates everything else.
	// BL0016 is suppressed for this class only: it is a pass-through IJSRuntime, not an interop call site.
#pragma warning disable BL0016
	sealed class FailingImportRuntime(IJSRuntime inner, string path) : IJSRuntime
	{
		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
			InvokeAsync<TValue>(identifier, CancellationToken.None, args);

		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
			identifier == "import" && args is [string requested] && requested == path
				? ValueTask.FromException<TValue>(new JSException("Failed to fetch dynamically imported module."))
				: inner.InvokeAsync<TValue>(identifier, cancellationToken, args);
	}
#pragma warning restore BL0016
}
