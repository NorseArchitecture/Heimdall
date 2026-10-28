using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using Norse.Abstractions.Contracts;
using Norse.AuthN.Components.FluentUI.Manage;
using Norse.AuthN.Services;

namespace Norse.AuthN.Components.FluentUI.Tests.Manage;

public sealed class ExternalLoginsTests : BunitContext
{
	readonly IAccountService _account = Substitute.For<IAccountService>();
	readonly ISessionTransition _sessionTransition = Substitute.For<ISessionTransition>();

	public ExternalLoginsTests()
	{
		Services.AddFluentUIComponents();
		// FluentUI components make JS interop calls bunit has no way to know about in advance —
		// loose mode is bunit's own documented answer (same rationale as LoginTests).
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services
			.AddSingleton(_account)
			.AddSingleton(_sessionTransition);
	}

	BunitNavigationManager Navigation => Services.GetRequiredService<BunitNavigationManager>();

	static ExternalLoginsResponse Logins(bool canRemove) =>
		new()
		{
			Logins = [new() { LoginProvider = "Google", ProviderKey = "g-1", DisplayName = "Google" }],
			AvailableProviders = [new() { Name = "Microsoft", DisplayName = "Microsoft Account" }],
			CanRemove = canRemove,
		};

	IRenderedComponent<ExternalLogins> RenderWith(ExternalLoginsResponse response)
	{
		_account.GetExternalLogins(Arg.Any<CancellationToken>())
			.Returns(Outcome<ExternalLoginsResponse>.Ok(response));
		return Render<ExternalLogins>();
	}

	[Fact]
	void Lists_registered_logins_and_available_providers()
	{
		var page = RenderWith(Logins(canRemove: true));

		page.Markup.ShouldContain("Google");
		page.Markup.ShouldContain("Microsoft Account");
		page.Find("[href]").GetAttribute("href")!.ShouldEndWith("LinkExternalLogin?provider=Microsoft");
	}

	[Fact]
	void Every_link_to_add_a_provider_forces_a_full_page_load()
	{
		var page = RenderWith(Logins(canRemove: true) with
		{
			AvailableProviders =
			[
				new() { Name = "Microsoft", DisplayName = "Microsoft Account" },
				new() { Name = "GitHub", DisplayName = "GitHub" },
			],
		});

		// LinkExternalLogin is a server endpoint that challenges the provider, not a routable page. FluentUI's
		// fluent-link navigates through Blazor.navigateTo on a same-origin click, passing its force-load
		// attribute as forceLoad, so without it the router answers Not Found on a live circuit.
		var links = page.FindAll("[href*=LinkExternalLogin]");
		links.Count.ShouldBe(2);
		links.ShouldAllBe(link => link.GetAttribute("force-load") == "true");
	}

	[Fact]
	void Renders_the_page_heading_like_its_siblings()
	{
		var page = RenderWith(Logins(canRemove: true));

		page.FindAll("h3")[0].TextContent.ShouldBe("Manage your external logins");
	}

	[Fact]
	void Hides_the_remove_button_when_the_server_says_it_cannot_be_removed()
	{
		var page = RenderWith(Logins(canRemove: false));

		page.Markup.ShouldContain("Google");
		page.Markup.ShouldNotContain("Remove");
		page.FindAll("[data-provider]").ShouldBeEmpty();
	}

	[Fact]
	void Removing_a_login_begins_one_transition()
	{
		NavigationResult next = new() { NextUrl = "/Account/Manage/ExternalLogins" };
		_account.RemoveExternalLogin(Arg.Any<RemoveExternalLoginRequest>(), Arg.Any<CancellationToken>())
			.Returns(Outcome<NavigationResult>.Ok(next));
		var page = RenderWith(Logins(canRemove: true));

		page.Find("[data-provider=Google]").Click();

		_account.Received(1).RemoveExternalLogin(
			Arg.Is<RemoveExternalLoginRequest>(r => r.LoginProvider == "Google" && r.ProviderKey == "g-1"),
			Arg.Any<CancellationToken>());
		_sessionTransition.Received(1).Begin(next);
		Navigation.History.ShouldBeEmpty();
	}

	[Fact]
	void A_refused_removal_renders_the_conflict_in_place()
	{
		_account.RemoveExternalLogin(Arg.Any<RemoveExternalLoginRequest>(), Arg.Any<CancellationToken>())
			.Returns(new Outcome<NavigationResult>(new Failed(
				Problem.ModelError(ErrorCategory.Conflict, "This is the only way to sign in to your account."))));
		var page = RenderWith(Logins(canRemove: true));

		page.Find("[data-provider=Google]").Click();

		page.Find("[role=alert]").TextContent.ShouldContain("This is the only way to sign in to your account.");
		_sessionTransition.DidNotReceiveWithAnyArgs().Begin(null!);
		// The refusal leaves the list standing — the user is still on the page that offered the removal.
		page.FindAll("[data-provider=Google]").ShouldHaveSingleItem();
	}

	[Fact]
	void A_bare_render_removes_nothing()
	{
		RenderWith(Logins(canRemove: true));

		_account.DidNotReceiveWithAnyArgs()
			.RemoveExternalLogin(default!, Xunit.TestContext.Current.CancellationToken);
		_sessionTransition.DidNotReceiveWithAnyArgs().Begin(null!);
	}
}
