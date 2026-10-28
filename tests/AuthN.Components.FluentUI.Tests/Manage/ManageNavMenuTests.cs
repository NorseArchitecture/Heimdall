using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Norse.AuthN.Components.FluentUI.Tests.Manage;

public sealed class ManageNavMenuTests : BunitContext
{
	public ManageNavMenuTests()
	{
		Services.AddFluentUIComponents();
		JSInterop.Mode = JSRuntimeMode.Loose;
	}

	// The external-logins link is unconditional (spec §6.4): the page itself says when nothing is linked.
	[Fact]
	void Links_every_manage_page()
	{
		var component = Render<ManageNavMenu>();

		component.FindAll("[href]").Select(a => a.GetAttribute("href")).ShouldBe(
		[
			"Account/Manage",
			"Account/Manage/Email",
			"Account/Manage/ChangePassword",
			"Account/Manage/ExternalLogins",
			"Account/Manage/TwoFactorAuthentication",
			"Account/Manage/Passkeys",
			"Account/Manage/PersonalData"
		]);
	}

	// Guard (T3-5): the nav stays static, so nothing routes its links but the page tree. A link whose page
	// moved or was renamed would 404 silently; this fails loud instead.
	[Fact]
	void Every_nav_link_resolves_to_a_routable_heimdall_page()
	{
		var routes = typeof(ManageLayout).Assembly.GetTypes()
			.SelectMany(t => t.GetCustomAttributes<RouteAttribute>())
			.Select(r => r.Template.TrimStart('/'))
			.ToHashSet(StringComparer.OrdinalIgnoreCase);

		var component = Render<ManageNavMenu>();

		foreach (var href in component.FindAll("[href]").Select(a => a.GetAttribute("href")))
			routes.ShouldContain(href!, href);
	}
}
