using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using Norse.Abstractions.Contracts;
using Norse.AuthN.Components.FluentUI.Manage;
using Norse.AuthN.Services;

namespace Norse.AuthN.Components.FluentUI.Tests.Manage;

// Derives from BunitContext, not InteractiveBunitContext: that base would overwrite the static
// renderer this fact is about. A prerender has no browser behind it, so the page asks nothing of
// passkeys.js there, and Add renders disabled until the interactive render learns support.
public sealed class PasskeysStaticRenderTests : BunitContext
{
	readonly IPasskeyService _passkeys = Substitute.For<IPasskeyService>();

	public PasskeysStaticRenderTests()
	{
		Services.AddFluentUIComponents();
		JSInterop.Mode = JSRuntimeMode.Loose;
		_passkeys.GetPasskeys(Arg.Any<CancellationToken>())
			.Returns(Outcome<PasskeysResponse>.Ok(new()
			{
				Passkeys = [new() { CredentialId = "a", Name = "Laptop", RenameUrl = "/Account/Manage/RenamePasskey/a" }],
			}));
		Services.AddSingleton(_passkeys);
		SetRendererInfo(new("Static", false));
	}

	[Fact]
	void A_static_prerender_makes_no_js_call()
	{
		var module = JSInterop.SetupModule(PasskeyModule.Path);

		var page = Render<Passkeys>();

		module.Invocations.ShouldBeEmpty();
		JSInterop.Invocations.ShouldNotContain(invocation => invocation.Identifier == "import");
		// The loader still runs, so the page arrives with its list.
		page.Markup.ShouldContain("Laptop");
		page.Find("fluent-button[data-action='add']").HasAttribute("disabled").ShouldBeTrue();
	}
}
