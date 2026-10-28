using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using Norse.Abstractions.Contracts;
using Norse.AuthN.Components.FluentUI.Manage;
using Norse.AuthN.Components.FluentUI.Shared;
using Norse.AuthN.Services;

namespace Norse.AuthN.Components.FluentUI.Tests.Manage;

public sealed class RecoveryCodesHandoffTests : InteractiveBunitContext
{
	const string Route = "Account/Manage/ShowRecoveryCodes";
	const string AlreadyShown = "These recovery codes have already been shown.";

	static readonly string[] _codes = ["AAAAA-11111", "BBBBB-22222", "CCCCC-33333"];

	readonly ITwoFactorService _twoFactor = Substitute.For<ITwoFactorService>();

	public RecoveryCodesHandoffTests()
	{
		Services.AddFluentUIComponents();
		// FluentUI components make JS interop calls bunit has no way to know about in advance —
		// loose mode is bunit's own documented answer (same rationale as LoginTests).
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddSingleton(_twoFactor);
	}

	BunitNavigationManager Navigation => Services.GetRequiredService<BunitNavigationManager>();

	IRenderedComponent<RecoveryCodesHandoff> RenderWith(string? handoff, Outcome<RecoveryCodesResponse>? reveal = null)
	{
		if (reveal is not null)
			_twoFactor.RevealRecoveryCodes(Arg.Any<RevealRecoveryCodesRequest>(), Arg.Any<CancellationToken>())
				.Returns(reveal);
		Navigation.NavigateTo(handoff is null ? Route : $"{Route}?handoff={Uri.EscapeDataString(handoff)}");
		return Render<RecoveryCodesHandoff>();
	}

	[Fact]
	void The_handoff_is_revealed_and_the_codes_render_with_the_verified_message()
	{
		var page = RenderWith("sealed", Outcome<RecoveryCodesResponse>.Ok(new() { RecoveryCodes = _codes }));

		_twoFactor.Received(1).RevealRecoveryCodes(Arg.Is<RevealRecoveryCodesRequest>(r => r.Handoff == "sealed"),
			Arg.Any<CancellationToken>());
		page.WaitForAssertion(() =>
			page.FindAll("code.recovery-code").Select(code => code.TextContent).ShouldBe(_codes));
		page.Find("[role=alert]").TextContent.Trim().ShouldBe("Your authenticator app has been verified.");
	}

	// bUnit's History is stack-ordered: the first entry is the latest navigation.
	[Fact]
	void The_history_entry_is_replaced_without_the_handoff()
	{
		RenderWith("sealed", Outcome<RecoveryCodesResponse>.Ok(new() { RecoveryCodes = _codes }));

		var latest = Navigation.History.First();
		latest.Uri.ShouldBe(Route);
		latest.Options.ReplaceHistoryEntry.ShouldBeTrue();
		Navigation.Uri.ShouldBe(Navigation.ToAbsoluteUri(Route).ToString());
	}

	[Fact]
	void A_failed_reveal_renders_the_problem_and_no_codes()
	{
		var page = RenderWith("sealed",
			Outcome<RecoveryCodesResponse>.Err(ErrorCategory.Fault, correlationId: Guid.Empty));

		page.WaitForAssertion(() =>
			page.Find("[role=alert]").TextContent.ShouldContain("Your recovery codes could not be shown."));
		page.FindAll("code.recovery-code").ShouldBeEmpty();
		page.FindComponents<ShowRecoveryCodes>().ShouldBeEmpty();
		AssertTheGenerateLinkIsOffered(page);
	}

	[Fact]
	void An_already_revealed_handoff_renders_the_already_shown_state()
	{
		var page = RenderWith("sealed",
			Outcome<RecoveryCodesResponse>.Err(ErrorCategory.Validation,
				new Dictionary<string, string[]> { [nameof(RevealRecoveryCodesRequest.Handoff)] = [AlreadyShown] }));

		page.WaitForAssertion(() =>
			page.Find("[role=alert]").TextContent.Trim().ShouldBe(AlreadyShown));
		page.FindAll("code.recovery-code").ShouldBeEmpty();
		page.FindComponents<ShowRecoveryCodes>().ShouldBeEmpty();
		AssertTheGenerateLinkIsOffered(page);
	}

	[Fact]
	void A_successful_reveal_offers_no_generate_link()
	{
		var page = RenderWith("sealed", Outcome<RecoveryCodesResponse>.Ok(new() { RecoveryCodes = _codes }));

		page.WaitForAssertion(() => page.FindAll("code.recovery-code").Count.ShouldBe(_codes.Length));
		page.FindAll("[href]").ShouldBeEmpty();
	}

	// A failed reveal leaves two-factor on with no codes in view, so the page offers the static sibling
	// link to a fresh set (spec §6.5) rather than a dead end.
	static void AssertTheGenerateLinkIsOffered(IRenderedComponent<RecoveryCodesHandoff> page)
	{
		var link = page.FindAll("[href]").ShouldHaveSingleItem();
		link.GetAttribute("href").ShouldBe("Account/Manage/GenerateRecoveryCodes");
		link.TextContent.Trim().ShouldBe("Generate new recovery codes");
	}

	[Fact]
	void No_handoff_renders_an_error_and_calls_nothing()
	{
		var page = RenderWith(null);

		page.Find("[role=alert]").TextContent.ShouldContain("There are no recovery codes to show.");
		page.FindAll("code.recovery-code").ShouldBeEmpty();
		_twoFactor.DidNotReceiveWithAnyArgs().RevealRecoveryCodes(default!, Xunit.TestContext.Current.CancellationToken);
		Navigation.History.First().Uri.ShouldBe(Route);
		Navigation.History.Count.ShouldBe(1);
	}

	// _Imports.razor imports Manage, so a page named ShowRecoveryCodes would shadow the shared panel
	// and render itself.
	[Fact]
	void The_handoff_page_and_the_shared_panel_are_distinct_types()
	{
		typeof(RecoveryCodesHandoff).ShouldNotBe(typeof(ShowRecoveryCodes));
		typeof(RecoveryCodesHandoff).Name.ShouldNotBe(typeof(ShowRecoveryCodes).Name);
		typeof(RecoveryCodesHandoff).GetCustomAttributes(typeof(RouteAttribute), false)
			.Cast<RouteAttribute>().ShouldHaveSingleItem().Template.ShouldBe("/Account/Manage/ShowRecoveryCodes");
		typeof(ShowRecoveryCodes).GetCustomAttributes(typeof(RouteAttribute), false).ShouldBeEmpty();
	}
}
