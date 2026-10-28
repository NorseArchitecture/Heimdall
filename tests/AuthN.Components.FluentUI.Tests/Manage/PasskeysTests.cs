using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;
using Norse.Abstractions.Contracts;
using Norse.AuthN.Components.FluentUI.Manage;
using Norse.AuthN.Services;

namespace Norse.AuthN.Components.FluentUI.Tests.Manage;

// passkeys.js is pinned here, through bUnit's module interop, and by the walk's virtual authenticator
// (T3-25): Node cannot exercise WebAuthn, so there is no JS harness.
public sealed class PasskeysTests : InteractiveBunitContext
{
	const string ModulePath = "./_content/Norse.AuthN.Components.FluentUI/passkeys.js";
	const string OptionsJson = """{"challenge":"c"}""";
	const string CredentialJson = """{"id":"cred"}""";
	const string RenameUrl = "/Account/Manage/RenamePasskey/new-id";
	const string Dismissed = "No passkey was provided by the authenticator.";

	readonly IPasskeyService _passkeys = Substitute.For<IPasskeyService>();

	public PasskeysTests()
	{
		Services.AddFluentUIComponents();
		// FluentUI components make JS interop calls bunit has no way to know about in advance —
		// loose mode is bunit's own documented answer (same rationale as LoginTests).
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddSingleton(_passkeys);
		_passkeys.GetPasskeyCreationOptions(Arg.Any<CancellationToken>())
			.Returns(Outcome<PasskeyCreationOptionsResponse>.Ok(new() { OptionsJson = OptionsJson, AttestationState = "state" }));
	}

	BunitNavigationManager Navigation => Services.GetRequiredService<BunitNavigationManager>();

	static PasskeyEntry Entry(string id, string name) =>
		new() { CredentialId = id, Name = name, RenameUrl = $"/Account/Manage/RenamePasskey/{id}" };

	void Listing(params PasskeyEntry[] entries) =>
		_passkeys.GetPasskeys(Arg.Any<CancellationToken>())
			.Returns(Outcome<PasskeysResponse>.Ok(new() { Passkeys = entries }));

	BunitJSModuleInterop Module(bool supported = true)
	{
		var module = JSInterop.SetupModule(ModulePath);
		module.Setup<bool>("isSupported").SetResult(supported);
		return module;
	}

	static void Ceremony(BunitJSModuleInterop module, string? credentialJson, string? error) =>
		module.Setup<PasskeyCeremonyResult>("create", OptionsJson).SetResult(new(credentialJson, error));

	[Fact]
	void The_list_renders_each_passkey_with_its_server_rename_url_and_delete()
	{
		Module();
		Listing(Entry("a", "Laptop"), Entry("b", ""));

		var page = Render<Passkeys>();

		page.Find("h3").TextContent.ShouldBe("Manage your passkeys");
		page.Markup.ShouldContain("Laptop");
		page.Markup.ShouldContain("Unnamed passkey");
		page.FindAll("[href*=RenamePasskey]").Select(link => link.GetAttribute("href"))
			.ShouldBe(["/Account/Manage/RenamePasskey/a", "/Account/Manage/RenamePasskey/b"]);
		page.FindAll("fluent-button[data-credential-id]").Select(button => button.GetAttribute("data-credential-id"))
			.ShouldBe(["a", "b"]);
		page.Find("fluent-button[data-action='add']").HasAttribute("disabled").ShouldBeFalse();
	}

	[Fact]
	void An_empty_list_says_so()
	{
		Module();
		Listing();

		var page = Render<Passkeys>();

		page.Markup.ShouldContain("No passkeys are registered.");
		page.FindAll("fluent-button[data-action='add']").ShouldHaveSingleItem();
	}

	[Fact]
	void An_unsupported_browser_hides_add_and_says_why()
	{
		Module(supported: false);
		Listing(Entry("a", "Laptop"));

		var page = Render<Passkeys>();

		page.Markup.ShouldContain("Some passkey features are missing. Please update your browser.");
		page.FindAll("fluent-button[data-action='add']").ShouldBeEmpty();
		// The list stays manageable: removing a passkey needs no WebAuthn.
		page.FindAll("fluent-button[data-credential-id]").ShouldHaveSingleItem();
	}

	[Fact]
	void A_dismissed_prompt_shows_the_authenticator_message_and_calls_nothing()
	{
		var module = Module();
		Ceremony(module, null, Dismissed);
		Listing();
		var page = Render<Passkeys>();

		page.Find("fluent-button[data-action='add']").Click();

		page.Find(".alert-danger").TextContent.Trim().ShouldBe(Dismissed);
		_passkeys.DidNotReceiveWithAnyArgs().AddPasskey(null!, Xunit.TestContext.Current.CancellationToken);
		Navigation.History.ShouldBeEmpty();
	}

	[Fact]
	void An_aborted_ceremony_calls_nothing_and_shows_nothing()
	{
		var module = Module();
		Ceremony(module, null, null);
		Listing();
		var page = Render<Passkeys>();

		page.Find("fluent-button[data-action='add']").Click();

		page.FindAll(".alert").ShouldBeEmpty();
		_passkeys.DidNotReceiveWithAnyArgs().AddPasskey(null!, Xunit.TestContext.Current.CancellationToken);
		Navigation.History.ShouldBeEmpty();
		// Nothing happened, so Add is offered again.
		page.Find("fluent-button[data-action='add']").HasAttribute("disabled").ShouldBeFalse();
	}

	[Fact]
	void A_completed_ceremony_adds_and_navigates_softly_to_the_server_url()
	{
		var module = Module();
		Ceremony(module, CredentialJson, null);
		_passkeys.AddPasskey(Arg.Any<AddPasskeyRequest>(), Arg.Any<CancellationToken>())
			.Returns(Outcome<NavigationResult>.Ok(new() { NextUrl = RenameUrl }));
		Listing();
		var page = Render<Passkeys>();

		page.Find("fluent-button[data-action='add']").Click();

		_passkeys.Received(1).AddPasskey(
			Arg.Is<AddPasskeyRequest>(r => r.CredentialJson == CredentialJson && r.AttestationState == "state"),
			Arg.Any<CancellationToken>());
		var navigation = Navigation.History.ShouldHaveSingleItem();
		navigation.Uri.ShouldBe(RenameUrl);
		navigation.Options.ForceLoad.ShouldBeFalse();
	}

	[Fact]
	void A_refused_add_renders_the_server_text_in_place()
	{
		const string Cap = "You have reached the maximum number of allowed passkeys.";
		var module = Module();
		Ceremony(module, CredentialJson, null);
		_passkeys.AddPasskey(Arg.Any<AddPasskeyRequest>(), Arg.Any<CancellationToken>())
			.Returns(new Outcome<NavigationResult>(new Failed(Problem.ModelError(ErrorCategory.Conflict, Cap))));
		Listing();
		var page = Render<Passkeys>();

		page.Find("fluent-button[data-action='add']").Click();

		page.Find(".alert-danger").TextContent.Trim().ShouldBe(Cap);
		Navigation.History.ShouldBeEmpty();
	}

	[Fact]
	void A_js_exception_renders_in_place()
	{
		var module = Module();
		module.Setup<PasskeyCeremonyResult>("create", OptionsJson).SetException(new JSException("The operation is insecure."));
		Listing();
		var page = Render<Passkeys>();

		page.Find("fluent-button[data-action='add']").Click();

		page.Find(".alert-danger").TextContent.Trim().ShouldBe("The operation is insecure.");
		_passkeys.DidNotReceiveWithAnyArgs().AddPasskey(null!, Xunit.TestContext.Current.CancellationToken);
		Navigation.History.ShouldBeEmpty();
	}

	[Fact]
	void Delete_removes_and_reloads_with_the_message()
	{
		Module();
		_passkeys.GetPasskeys(Arg.Any<CancellationToken>())
			.Returns(
				Outcome<PasskeysResponse>.Ok(new() { Passkeys = [Entry("a", "Laptop"), Entry("b", "Phone")] }),
				Outcome<PasskeysResponse>.Ok(new() { Passkeys = [Entry("b", "Phone")] }));
		_passkeys.RemovePasskey(Arg.Any<RemovePasskeyRequest>(), Arg.Any<CancellationToken>())
			.Returns(Outcome<BoolResponse>.Ok(new() { Value = true }));
		var page = Render<Passkeys>();

		page.Find("fluent-button[data-credential-id='a']").Click();

		_passkeys.Received(1).RemovePasskey(Arg.Is<RemovePasskeyRequest>(r => r.CredentialId == "a"), Arg.Any<CancellationToken>());
		_passkeys.Received(2).GetPasskeys(Arg.Any<CancellationToken>());
		page.Find(".alert-success").TextContent.Trim().ShouldBe("Passkey deleted successfully.");
		page.Markup.ShouldNotContain("Laptop");
		page.FindAll("fluent-button[data-credential-id]").ShouldHaveSingleItem();
		Navigation.History.ShouldBeEmpty();
	}

	[Fact]
	void A_failed_delete_renders_in_place_and_keeps_the_list()
	{
		Module();
		Listing(Entry("a", "Laptop"));
		_passkeys.RemovePasskey(Arg.Any<RemovePasskeyRequest>(), Arg.Any<CancellationToken>())
			.Returns(Outcome<BoolResponse>.Err(ErrorCategory.Fault, correlationId: Guid.Empty));
		var page = Render<Passkeys>();

		page.Find("fluent-button[data-credential-id='a']").Click();

		page.Find(".alert-danger").TextContent.Trim().ShouldBe("The passkey could not be deleted.");
		page.FindAll(".alert-success").ShouldBeEmpty();
		_passkeys.Received(1).GetPasskeys(Arg.Any<CancellationToken>());
		page.FindAll("fluent-button[data-credential-id='a']").ShouldHaveSingleItem();
	}

	[Fact]
	void A_failed_load_renders_in_place()
	{
		Module();
		_passkeys.GetPasskeys(Arg.Any<CancellationToken>())
			.Returns(Outcome<PasskeysResponse>.Err(ErrorCategory.Fault, correlationId: Guid.Empty));

		var page = Render<Passkeys>();

		page.Find(".alert-danger").TextContent.Trim().ShouldBe("Your passkeys could not be loaded.");
		page.FindAll("fluent-button").ShouldBeEmpty();
	}

	[Fact]
	void At_the_cap_add_is_hidden()
	{
		Module();
		Listing([.. Enumerable.Range(0, 100).Select(i => Entry($"id-{i}", $"Key {i}"))]);

		var page = Render<Passkeys>();

		page.Markup.ShouldContain("You have reached the maximum number of allowed passkeys.");
		page.FindAll("fluent-button[data-action='add']").ShouldBeEmpty();
	}

	[Fact]
	void Below_the_cap_add_is_offered()
	{
		Module();
		Listing([.. Enumerable.Range(0, 99).Select(i => Entry($"id-{i}", $"Key {i}"))]);

		var page = Render<Passkeys>();

		page.Markup.ShouldNotContain("You have reached the maximum number of allowed passkeys.");
		page.FindAll("fluent-button[data-action='add']").ShouldHaveSingleItem();
	}

	[Fact]
	void A_bare_render_performs_nothing()
	{
		Module();
		Listing(Entry("a", "Laptop"));

		Render<Passkeys>();

		_passkeys.DidNotReceiveWithAnyArgs().GetPasskeyCreationOptions(Xunit.TestContext.Current.CancellationToken);
		_passkeys.DidNotReceiveWithAnyArgs().AddPasskey(null!, Xunit.TestContext.Current.CancellationToken);
		_passkeys.DidNotReceiveWithAnyArgs().RemovePasskey(null!, Xunit.TestContext.Current.CancellationToken);
	}

	// The Add lock row alone cannot fail (a static render never learns support, so Add is disabled either way);
	// this pins both guards on a live circuit: CanAct while a dispatch is in flight, _busy across the ceremony.
	[Fact]
	async Task Add_and_every_delete_are_disabled_while_an_add_is_in_flight()
	{
		var module = Module();
		var ceremony = module.Setup<PasskeyCeremonyResult>("create", OptionsJson);
		TaskCompletionSource<Outcome<PasskeyCreationOptionsResponse>> options = new();
		_passkeys.GetPasskeyCreationOptions(Arg.Any<CancellationToken>()).Returns(options.Task);
		Listing(Entry("a", "Laptop"), Entry("b", "Phone"));
		var page = Render<Passkeys>();

		var click = page.Find("fluent-button[data-action='add']").ClickAsync(new());

		AllDisabled(page);

		await page.InvokeAsync(() => options.SetResult(
			Outcome<PasskeyCreationOptionsResponse>.Ok(new() { OptionsJson = OptionsJson, AttestationState = "state" })));

		page.WaitForAssertion(() => module.Invocations.ShouldContain(invocation => invocation.Identifier == "create"));
		AllDisabled(page);

		ceremony.SetResult(new(null, null));
		await click;
		page.Find("fluent-button[data-action='add']").HasAttribute("disabled").ShouldBeFalse();
	}

	static void AllDisabled(IRenderedComponent<Passkeys> page)
	{
		page.Find("fluent-button[data-action='add']").HasAttribute("disabled").ShouldBeTrue("add");
		page.FindAll("fluent-button[data-credential-id]").ShouldAllBe(button => button.HasAttribute("disabled"));
	}

	[Fact]
	void A_failed_module_import_renders_in_place_and_leaves_delete_usable()
	{
		// bUnit routes every import through SetupModule, which cannot fail; a decorator over its runtime can.
		Services.AddSingleton<IJSRuntime>(new FailingImportRuntime(JSInterop.JSRuntime, ModulePath));
		Listing(Entry("a", "Laptop"));

		var page = Render<Passkeys>();

		page.Find("[data-module-problem]").TextContent.Trim().ShouldBe("Failed to fetch dynamically imported module.");
		page.Find("fluent-button[data-action='add']").HasAttribute("disabled").ShouldBeTrue();
		page.Markup.ShouldNotContain("Some passkey features are missing. Please update your browser.");
		page.Find("fluent-button[data-credential-id='a']").HasAttribute("disabled").ShouldBeFalse();
	}

	[Fact]
	void A_dropped_circuit_during_the_support_probe_hides_add()
	{
		var module = JSInterop.SetupModule(ModulePath);
		module.Setup<bool>("isSupported").SetException(new JSDisconnectedException("The circuit is gone."));
		Listing(Entry("a", "Laptop"));

		var page = Render<Passkeys>();

		page.FindAll("fluent-button[data-action='add']").ShouldBeEmpty();
		page.FindAll("[data-module-problem]").ShouldBeEmpty();
	}

	[Fact]
	void A_deleted_passkey_leaves_the_list_even_when_the_reload_fails()
	{
		Module();
		_passkeys.GetPasskeys(Arg.Any<CancellationToken>())
			.Returns(
				Outcome<PasskeysResponse>.Ok(new() { Passkeys = [Entry("a", "Laptop"), Entry("b", "Phone")] }),
				Outcome<PasskeysResponse>.Err(ErrorCategory.Fault, correlationId: Guid.Empty));
		_passkeys.RemovePasskey(Arg.Any<RemovePasskeyRequest>(), Arg.Any<CancellationToken>())
			.Returns(Outcome<BoolResponse>.Ok(new() { Value = true }));
		var page = Render<Passkeys>();

		page.Find("fluent-button[data-credential-id='a']").Click();

		page.Find(".alert-success").TextContent.Trim().ShouldBe("Passkey deleted successfully.");
		page.Find(".alert-danger").TextContent.Trim().ShouldBe("Your passkeys could not be loaded.");
		page.Markup.ShouldNotContain("Laptop");
		page.FindAll("fluent-button[data-credential-id]").Select(button => button.GetAttribute("data-credential-id"))
			.ShouldBe(["b"]);
	}

	// A browser that answers after the page is gone: nothing is sent on its behalf.
	[Fact]
	async Task A_page_disposed_mid_ceremony_calls_nothing_and_throws_nothing()
	{
		var module = Module();
		var ceremony = module.Setup<PasskeyCeremonyResult>("create", OptionsJson);
		Listing();
		var page = Render<Passkeys>();
		var click = page.Find("fluent-button[data-action='add']").ClickAsync(new());
		page.WaitForAssertion(() => module.Invocations.ShouldContain(invocation => invocation.Identifier == "create"));

		await page.InvokeAsync(async () => await page.Instance.DisposeAsync());
		ceremony.SetResult(new(CredentialJson, null));

		await Should.NotThrowAsync(click);
		_ = _passkeys.DidNotReceiveWithAnyArgs().AddPasskey(null!, Xunit.TestContext.Current.CancellationToken);
		Navigation.History.ShouldBeEmpty();
	}

	// Fails the import of one module path, as a missing asset or a CSP block would; delegates everything else.
	// BL0016 is suppressed for this class only: it is a pass-through IJSRuntime, not an interop call site, and
	// the caller's own guard is what the fact exercises.
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
