using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using Norse.Abstractions.Contracts;
using Norse.AuthN.Components.FluentUI.Manage;
using Norse.AuthN.Services;

namespace Norse.AuthN.Components.FluentUI.Tests.Manage;

public sealed class ResetAuthenticatorTests : InteractiveBunitContext
{
	const string NextUrl = "/Account/Manage/EnableAuthenticator?status=authenticator-reset";

	readonly ITwoFactorService _twoFactor = Substitute.For<ITwoFactorService>();
	readonly ISessionTransition _sessionTransition = Substitute.For<ISessionTransition>();

	public ResetAuthenticatorTests()
	{
		Services.AddFluentUIComponents();
		// FluentUI components make JS interop calls bunit has no way to know about in advance —
		// loose mode is bunit's own documented answer (same rationale as LoginTests).
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services
			.AddSingleton(_twoFactor)
			.AddSingleton(_sessionTransition);
	}

	BunitNavigationManager Navigation => Services.GetRequiredService<BunitNavigationManager>();

	[Fact]
	void A_bare_render_performs_nothing()
	{
		var page = Render<ResetAuthenticator>();

		_twoFactor.DidNotReceiveWithAnyArgs().ResetAuthenticator(Xunit.TestContext.Current.CancellationToken);
		_sessionTransition.DidNotReceiveWithAnyArgs().Begin(null!);
		page.Find("fluent-message-bar").TextContent.Trim()
			.ShouldBe("If you reset your authenticator key your authenticator app will not work until you reconfigure it.");
		page.Find("fluent-button").TextContent.Trim().ShouldBe("Reset authenticator key");
	}

	[Fact]
	void Success_begins_one_transition_with_the_server_url()
	{
		_twoFactor.ResetAuthenticator(Arg.Any<CancellationToken>())
			.Returns(Outcome<NavigationResult>.Ok(new() { NextUrl = NextUrl }));
		var page = Render<ResetAuthenticator>();

		page.Find("fluent-button").Click();

		_twoFactor.Received(1).ResetAuthenticator(Arg.Any<CancellationToken>());
		_sessionTransition.Received(1).Begin(Arg.Is<NavigationResult>(r => r.NextUrl == NextUrl));
		Navigation.History.ShouldBeEmpty();
	}

	[Fact]
	void A_validation_failure_renders_in_place()
	{
		_twoFactor.ResetAuthenticator(Arg.Any<CancellationToken>())
			.Returns(Outcome<NavigationResult>.Err(ErrorCategory.Validation,
				new Dictionary<string, string[]> { [string.Empty] = ["The authenticator key could not be reset."] }));
		var page = Render<ResetAuthenticator>();

		page.Find("fluent-button").Click();

		page.Find("[role=alert]").TextContent.Trim().ShouldBe("The authenticator key could not be reset.");
		_sessionTransition.DidNotReceiveWithAnyArgs().Begin(null!);
		Navigation.History.ShouldBeEmpty();
		// The failure leaves the page standing — the user is still on the page that offered the action.
		page.FindAll("fluent-button").ShouldHaveSingleItem();
	}

	[Fact]
	void A_fault_renders_a_generic_line_in_place()
	{
		_twoFactor.ResetAuthenticator(Arg.Any<CancellationToken>())
			.Returns(Outcome<NavigationResult>.Err(ErrorCategory.Fault, correlationId: Guid.Empty));
		var page = Render<ResetAuthenticator>();

		page.Find("fluent-button").Click();

		page.Find("[role=alert]").TextContent.ShouldContain("Your authenticator key could not be reset.");
		_sessionTransition.DidNotReceiveWithAnyArgs().Begin(null!);
	}
}
