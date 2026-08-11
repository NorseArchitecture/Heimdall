using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
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
		entry.Options.ReplaceHistoryEntry.ShouldBeTrue();
		navigation.Uri.ShouldBe(navigation.ToAbsoluteUri("Account/Login?returnUrl=%2F").ToString());
	}

	// BunitNavigationManager cannot prove this: its constructor hardcodes
	// Initialize("http://localhost/", "http://localhost/") with no public API in bUnit 2.9.0 to give
	// it a non-root BaseUri, so ToBaseRelativePath against a root base never strips anything -- the
	// old buggy code and the fix produce the same output in that harness. RecordingNavigationManager
	// (same pattern as SessionTransitionTests.RecordingNavigationManager) calls the protected
	// Initialize overload directly with a genuinely non-root base, which is the actual precondition
	// Finding 1 was about, and overrides NavigateToCore to capture the requested URI instead of
	// navigating for real. Registering it as the NavigationManager service overrides bUnit's own
	// factory-based registration: BunitServiceProvider resolves services lazily on first GetService
	// call, and plain ServiceCollection.Add semantics mean the last registration for a given service
	// type wins -- so as long as this AddSingleton call happens before Render (the first resolution),
	// this instance is what RedirectToLogin receives, not BunitNavigationManager.
	sealed class RecordingNavigationManager : NavigationManager
	{
		internal string? RequestedUri { get; private set; }
		internal NavigationOptions RequestedOptions { get; private set; }

		public RecordingNavigationManager() =>
			Initialize("http://localhost/app/", "http://localhost/app/orders");

		protected override void NavigateToCore(string uri, NavigationOptions options)
		{
			RequestedUri = uri;
			RequestedOptions = options;
		}
	}

	// Proves the actual claim Finding 1 is about: with a genuinely non-root base URI
	// (http://localhost/app/), the fixed component's PathAndQuery-based return URL keeps the "/app"
	// segment. Against the old ToBaseRelativePath-based code this fails -- that code strips everything
	// up to and including BaseUri, so "http://localhost/app/orders" against base
	// "http://localhost/app/" relativizes to "orders", and the redirect target becomes
	// "Account/Login?returnUrl=%2Forders", not "%2Fapp%2Forders" (Codex review, PR #52).
	[Fact]
	void Preserves_the_apps_base_path_in_the_return_url()
	{
		RecordingNavigationManager navigation = new();
		Services.AddSingleton<NavigationManager>(navigation);

		Render<RedirectToLogin>();

		navigation.RequestedOptions.ForceLoad.ShouldBeFalse();
		navigation.RequestedOptions.ReplaceHistoryEntry.ShouldBeTrue();
		navigation.RequestedUri.ShouldBe("Account/Login?returnUrl=%2Fapp%2Forders");
	}
}
