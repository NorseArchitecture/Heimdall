using Microsoft.AspNetCore.Authorization;
using Norse.Abstractions.Components.Authorization;

namespace Norse.AuthN.Services;

/// <summary>
/// Named authorization policies for the AuthN service surface. <see cref="Public"/> is satisfied by
/// any principal, anonymous-role cookie included — Login/Register/Logout must still declare a policy
/// per decided law item 4, even though that policy imposes no real requirement.
/// </summary>
public static class AuthNPolicies
{
	/// <summary>Satisfied by any principal, the anonymous role included — never an empty one.</summary>
	public const string Public = "AuthN.Public";

	/// <summary>Configures <see cref="Public" />.</summary>
	/// <param name="policy">The builder to configure.</param>
	[NorsePolicy(Public)]
	public static void ConfigurePublic(AuthorizationPolicyBuilder policy) =>
		// "Any principal, anonymous role included" -- which is what Public always meant, and now says. The
		// prior RequireAssertion(_ => true) passed an unauthenticated empty principal too, which is the hole
		// the principal-at-the-door design closes.
		policy.RequireAuthenticatedUser();
}
