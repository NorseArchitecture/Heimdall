using Bunit;
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
}
