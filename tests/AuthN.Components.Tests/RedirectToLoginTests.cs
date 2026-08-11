using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;

namespace Norse.AuthN.Components.Tests;

public sealed class RedirectToLoginTests : BunitContext
{
	// No setup navigation, deliberately: bUnit's History is stack-ordered (latest first), and a
	// second entry invites asserting on the wrong one. Rendering at the base URI leaves exactly one
	// entry — the redirect itself — so ShouldHaveSingleItem is both the selector and the proof that
	// nothing else navigated.
	[Fact]
	void Redirects_softly_to_the_gate_preserving_the_return_url()
	{
		var navigation = Services.GetRequiredService<BunitNavigationManager>();

		Render<RedirectToLogin>();

		var entry = navigation.History.ShouldHaveSingleItem();
		entry.Options.ForceLoad.ShouldBeFalse();
		navigation.Uri.ShouldBe(navigation.ToAbsoluteUri("Account/Login?returnUrl=%2F").ToString());
	}
}
