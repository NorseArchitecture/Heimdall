using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using Norse.AuthN.Components.FluentUI.Manage;
using Norse.AuthN.Services;

namespace Norse.AuthN.Components.FluentUI.Tests.Manage;

// Derives from BunitContext, not InteractiveBunitContext: that base would overwrite the static
// renderer this fact is about. A prerender is followed by the circuit's own render of a fresh
// instance, so a reveal here would spend the one-shot handoff on a page that is about to be replaced.
public sealed class RecoveryCodesHandoffStaticRenderTests : BunitContext
{
	const string Route = "Account/Manage/ShowRecoveryCodes";

	readonly ITwoFactorService _twoFactor = Substitute.For<ITwoFactorService>();

	public RecoveryCodesHandoffStaticRenderTests()
	{
		Services.AddFluentUIComponents();
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddSingleton(_twoFactor);
		SetRendererInfo(new("Static", false));
	}

	BunitNavigationManager Navigation => Services.GetRequiredService<BunitNavigationManager>();

	[Fact]
	void A_prerender_reveals_nothing_and_keeps_the_handoff_in_the_url()
	{
		Navigation.NavigateTo($"{Route}?handoff=sealed");

		var page = Render<RecoveryCodesHandoff>();

		_twoFactor.DidNotReceiveWithAnyArgs().RevealRecoveryCodes(default!, Xunit.TestContext.Current.CancellationToken);
		page.Find(".norse-loader").ShouldNotBeNull();
		page.FindAll("code.recovery-code").ShouldBeEmpty();
		// Only the test's own navigation: the page replaced no history entry.
		Navigation.History.ShouldHaveSingleItem().Uri.ShouldBe($"{Route}?handoff=sealed");
		Navigation.Uri.ShouldBe(Navigation.ToAbsoluteUri($"{Route}?handoff=sealed").ToString());
	}
}
