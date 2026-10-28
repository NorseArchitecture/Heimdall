using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using Norse.Abstractions.Contracts;
using Norse.AuthN.Components.FluentUI.Manage;
using Norse.AuthN.Services;

namespace Norse.AuthN.Components.FluentUI.Tests.Manage;

public sealed class TwoFactorAuthenticationTests : InteractiveBunitContext
{
	readonly IAccountService _account = Substitute.For<IAccountService>();
	readonly ITwoFactorService _twoFactor = Substitute.For<ITwoFactorService>();
	readonly ISessionTransition _sessionTransition = Substitute.For<ISessionTransition>();

	public TwoFactorAuthenticationTests()
	{
		Services.AddFluentUIComponents();
		// FluentUI components make JS interop calls bunit has no way to know about in advance —
		// loose mode is bunit's own documented answer (same rationale as LoginTests).
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services
			.AddSingleton(_account)
			.AddSingleton(_twoFactor)
			.AddSingleton(_sessionTransition);
	}

	BunitNavigationManager Navigation => Services.GetRequiredService<BunitNavigationManager>();

	static AccountStatusResponse Status(bool twoFactorEnabled = true, bool hasAuthenticator = true, int recoveryCodesLeft = 10, bool isMachineRemembered = false) =>
		new()
		{
			HasPassword = true,
			EmailConfirmed = true,
			TwoFactorEnabled = twoFactorEnabled,
			HasAuthenticator = hasAuthenticator,
			RecoveryCodesLeft = recoveryCodesLeft,
			IsMachineRemembered = isMachineRemembered,
			PasskeyCount = 0,
			ExternalLoginCount = 0,
		};

	IRenderedComponent<TwoFactorAuthentication> RenderWith(AccountStatusResponse status)
	{
		_account.GetAccountStatus(Arg.Any<CancellationToken>())
			.Returns(Outcome<AccountStatusResponse>.Ok(status));
		return Render<TwoFactorAuthentication>();
	}

	[Fact]
	void Two_factor_off_shows_only_the_add_authenticator_link()
	{
		var page = RenderWith(Status(twoFactorEnabled: false, hasAuthenticator: false, recoveryCodesLeft: 0));

		var link = page.FindAll("[href]").ShouldHaveSingleItem();
		link.GetAttribute("href").ShouldBe("Account/Manage/EnableAuthenticator");
		link.TextContent.ShouldBe("Add authenticator app");
		page.Markup.ShouldNotContain("recovery code");
		page.FindAll("fluent-button").ShouldBeEmpty();
	}

	[Fact]
	void Two_factor_on_links_disable_reset_codes_set_up_and_reset_authenticator()
	{
		var page = RenderWith(Status());

		page.FindAll("[href]").Select(link => (link.GetAttribute("href"), link.TextContent)).ShouldBe(
		[
			("Account/Manage/Disable2fa", "Disable 2FA"),
			("Account/Manage/GenerateRecoveryCodes", "Reset recovery codes"),
			("Account/Manage/EnableAuthenticator", "Set up authenticator app"),
			("Account/Manage/ResetAuthenticator", "Reset authenticator app"),
		]);
	}

	[Theory]
	[InlineData(0, "You have no recovery codes left.")]
	[InlineData(1, "You have 1 recovery code left.")]
	[InlineData(3, "You have 3 recovery codes left.")]
	[InlineData(4, null)]
	void The_recovery_code_warning_matches_the_count(int recoveryCodesLeft, string? warning)
	{
		var page = RenderWith(Status(recoveryCodesLeft: recoveryCodesLeft));

		var bars = page.FindAll("fluent-message-bar");
		if (warning is null)
		{
			bars.ShouldBeEmpty();
			page.FindAll("[href='Account/Manage/GenerateRecoveryCodes']").Count.ShouldBe(1);
			return;
		}

		bars.ShouldHaveSingleItem().TextContent.Trim().ShouldBe(warning);
		// The warning carries its own link to a fresh set, beside the standing "Reset recovery codes" link.
		page.FindAll("[href='Account/Manage/GenerateRecoveryCodes']").Count.ShouldBe(2);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	void Forget_this_browser_is_shown_only_when_the_machine_is_remembered(bool isMachineRemembered)
	{
		var page = RenderWith(Status(isMachineRemembered: isMachineRemembered));

		page.FindAll("fluent-button").Count.ShouldBe(isMachineRemembered ? 1 : 0);
		page.Markup.Contains("Forget this browser").ShouldBe(isMachineRemembered);
	}

	// The remember-machine cookie outlives a disabled 2FA, so the page still offers to clear it.
	[Fact]
	void Forget_this_browser_is_shown_when_two_factor_is_off_and_the_machine_is_remembered()
	{
		var page = RenderWith(Status(twoFactorEnabled: false, isMachineRemembered: true));

		page.FindAll("fluent-button").ShouldHaveSingleItem().TextContent.Trim().ShouldBe("Forget this browser");
	}

	[Fact]
	void Forget_this_browser_begins_one_transition_with_the_server_url()
	{
		NavigationResult next = new() { NextUrl = "/Account/Manage/TwoFactorAuthentication?status=browser-forgotten" };
		_twoFactor.ForgetTwoFactorClient(Arg.Any<CancellationToken>())
			.Returns(Outcome<NavigationResult>.Ok(next));
		var page = RenderWith(Status(isMachineRemembered: true));

		page.Find("fluent-button").Click();

		_twoFactor.Received(1).ForgetTwoFactorClient(Arg.Any<CancellationToken>());
		_sessionTransition.Received(1).Begin(Arg.Is<NavigationResult>(r => r.NextUrl == "/Account/Manage/TwoFactorAuthentication?status=browser-forgotten"));
		Navigation.History.ShouldBeEmpty();
	}

	[Fact]
	void A_failed_forget_renders_the_problem_and_begins_nothing()
	{
		_twoFactor.ForgetTwoFactorClient(Arg.Any<CancellationToken>())
			.Returns(Outcome<NavigationResult>.Err(ErrorCategory.Fault, correlationId: Guid.Empty));
		var page = RenderWith(Status(isMachineRemembered: true));

		page.Find("fluent-button").Click();

		page.Find("[role=alert]").TextContent.ShouldContain("This browser could not be forgotten.");
		_sessionTransition.DidNotReceiveWithAnyArgs().Begin(null!);
		// The failure leaves the page standing — the user is still on the page that offered the action.
		page.FindAll("fluent-button").ShouldHaveSingleItem();
	}

	[Fact]
	void A_bare_render_forgets_nothing()
	{
		RenderWith(Status(isMachineRemembered: true));

		_twoFactor.DidNotReceiveWithAnyArgs().ForgetTwoFactorClient(Xunit.TestContext.Current.CancellationToken);
		_sessionTransition.DidNotReceiveWithAnyArgs().Begin(null!);
	}

	[Fact]
	void A_failed_status_load_renders_the_problem()
	{
		_account.GetAccountStatus(Arg.Any<CancellationToken>())
			.Returns(Outcome<AccountStatusResponse>.Err(ErrorCategory.Fault, correlationId: Guid.Empty));

		var page = Render<TwoFactorAuthentication>();

		page.Find("[role=alert]").TextContent.ShouldContain("Your two-factor settings could not be loaded.");
		page.FindAll("[href]").ShouldBeEmpty();
		page.FindAll("fluent-button").ShouldBeEmpty();
	}
}
