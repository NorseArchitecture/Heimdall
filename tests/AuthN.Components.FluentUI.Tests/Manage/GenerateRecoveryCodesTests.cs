using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using Norse.Abstractions.Contracts;
using Norse.AuthN.Components.FluentUI.Manage;
using Norse.AuthN.Components.FluentUI.Shared;
using Norse.AuthN.Services;

namespace Norse.AuthN.Components.FluentUI.Tests.Manage;

public sealed class GenerateRecoveryCodesTests : InteractiveBunitContext
{
	const string NotEnabled = "Cannot generate recovery codes because two-factor authentication is not enabled.";

	static readonly string[] _codes = ["AAAAA-11111", "BBBBB-22222", "CCCCC-33333"];

	readonly ITwoFactorService _twoFactor = Substitute.For<ITwoFactorService>();

	public GenerateRecoveryCodesTests()
	{
		Services.AddFluentUIComponents();
		// FluentUI components make JS interop calls bunit has no way to know about in advance —
		// loose mode is bunit's own documented answer (same rationale as LoginTests).
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddSingleton(_twoFactor);
	}

	BunitNavigationManager Navigation => Services.GetRequiredService<BunitNavigationManager>();

	[Fact]
	void A_bare_render_performs_nothing()
	{
		var page = Render<GenerateRecoveryCodes>();

		_twoFactor.DidNotReceiveWithAnyArgs().GenerateRecoveryCodes(Xunit.TestContext.Current.CancellationToken);
		page.FindComponents<ShowRecoveryCodes>().ShouldBeEmpty();
		page.Find("fluent-message-bar").TextContent.Trim().ShouldBe("Put these codes in a safe place.");
		page.Find("fluent-button").TextContent.Trim().ShouldBe("Generate Recovery Codes");
	}

	// Generate rotates no stamp, so the codes render in place: no navigation, no transition.
	[Fact]
	void Success_renders_the_codes_in_place_with_the_generated_message()
	{
		_twoFactor.GenerateRecoveryCodes(Arg.Any<CancellationToken>())
			.Returns(Outcome<RecoveryCodesResponse>.Ok(new() { RecoveryCodes = _codes }));
		var page = Render<GenerateRecoveryCodes>();

		page.Find("fluent-button").Click();

		_twoFactor.Received(1).GenerateRecoveryCodes(Arg.Any<CancellationToken>());
		page.FindAll("code.recovery-code").Select(code => code.TextContent).ShouldBe(_codes);
		page.Find("[role=alert]").TextContent.Trim().ShouldBe("You have generated new recovery codes.");
		page.FindAll("fluent-button").ShouldBeEmpty();
		Navigation.History.ShouldBeEmpty();
	}

	[Fact]
	void A_validation_failure_renders_in_place()
	{
		_twoFactor.GenerateRecoveryCodes(Arg.Any<CancellationToken>())
			.Returns(Outcome<RecoveryCodesResponse>.Err(ErrorCategory.Validation,
				new Dictionary<string, string[]> { [string.Empty] = [NotEnabled] }));
		var page = Render<GenerateRecoveryCodes>();

		page.Find("fluent-button").Click();

		page.Find("[role=alert]").TextContent.Trim().ShouldBe(NotEnabled);
		page.FindComponents<ShowRecoveryCodes>().ShouldBeEmpty();
		Navigation.History.ShouldBeEmpty();
		// The failure leaves the page standing — the user is still on the page that offered the action.
		page.FindAll("fluent-button").ShouldHaveSingleItem();
	}

	[Fact]
	void A_fault_renders_a_generic_line_in_place()
	{
		_twoFactor.GenerateRecoveryCodes(Arg.Any<CancellationToken>())
			.Returns(Outcome<RecoveryCodesResponse>.Err(ErrorCategory.Fault, correlationId: Guid.Empty));
		var page = Render<GenerateRecoveryCodes>();

		page.Find("fluent-button").Click();

		page.Find("[role=alert]").TextContent.ShouldContain("Your recovery codes could not be generated.");
		page.FindComponents<ShowRecoveryCodes>().ShouldBeEmpty();
	}
}
