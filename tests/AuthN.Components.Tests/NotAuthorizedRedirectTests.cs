using System.Security.Claims;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Norse.Abstractions.Components.Authorization;

namespace Norse.AuthN.Components.Tests;

public sealed class NotAuthorizedRedirectTests : BunitContext
{
	// No setup navigation, as in RedirectToLoginTests: rendering at the base URI leaves exactly one
	// History entry — the redirect itself — so ShouldHaveSingleItem is both the selector and the proof
	// that nothing else navigated.
	[Fact]
	void An_identity_principal_is_sent_to_access_denied_replacing_history()
	{
		var navigation = Services.GetRequiredService<BunitNavigationManager>();

		Render<NotAuthorizedRedirect>(p => p.Add(r => r.User, Principal("Identity.Application")));

		var entry = navigation.History.ShouldHaveSingleItem();
		entry.Options.ForceLoad.ShouldBeFalse();
		entry.Options.ReplaceHistoryEntry.ShouldBeTrue();
		navigation.Uri.ShouldBe(navigation.ToAbsoluteUri("Account/AccessDenied?ReturnUrl=%2F").ToString());
	}

	// The in-circuit lane carries the return URL the HTTP lane's cookie redirect does (Midgard), as the
	// PathBase-qualified path and query of the forbidden route: local by construction.
	[Fact]
	void An_identity_principal_carries_the_forbidden_route_as_the_return_url()
	{
		var navigation = Services.GetRequiredService<BunitNavigationManager>();
		navigation.NavigateTo("Account/Manage?tab=passkeys");

		Render<NotAuthorizedRedirect>(p => p.Add(r => r.User, Principal("Identity.Application")));

		navigation.Uri.ShouldBe(navigation.ToAbsoluteUri("Account/AccessDenied?ReturnUrl=%2FAccount%2FManage%3Ftab%3Dpasskeys").ToString());
	}

	// The anonymous principal is authenticated (Midgard's NorseAnonymousHandler mints it), so
	// IsAuthenticated alone would send it to AccessDenied — it has no account to be denied with.
	[Fact]
	void An_anonymous_role_principal_is_sent_to_login_with_the_return_url()
	{
		var navigation = Services.GetRequiredService<BunitNavigationManager>();

		Render<NotAuthorizedRedirect>(p => p.Add(r => r.User, Principal("Anonymous", new Claim(ClaimTypes.Role, NorseRoles.Anonymous))));

		var entry = navigation.History.ShouldHaveSingleItem();
		entry.Options.ReplaceHistoryEntry.ShouldBeTrue();
		navigation.Uri.ShouldBe(navigation.ToAbsoluteUri("Account/Login?returnUrl=%2F").ToString());
	}

	[Fact]
	void An_unauthenticated_principal_is_sent_to_login_with_the_return_url()
	{
		var navigation = Services.GetRequiredService<BunitNavigationManager>();

		Render<NotAuthorizedRedirect>(p => p.Add(r => r.User, new ClaimsPrincipal(new ClaimsIdentity())));

		var entry = navigation.History.ShouldHaveSingleItem();
		entry.Options.ReplaceHistoryEntry.ShouldBeTrue();
		navigation.Uri.ShouldBe(navigation.ToAbsoluteUri("Account/Login?returnUrl=%2F").ToString());
	}

	static ClaimsPrincipal Principal(string authenticationType, params Claim[] claims) =>
		new(new ClaimsIdentity([new(ClaimTypes.Name, "user@example.com"), .. claims], authenticationType));
}
