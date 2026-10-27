using Microsoft.AspNetCore.Authorization;
using Norse.Abstractions.Components.Authorization;

namespace Norse.AuthN.Services;

/// <summary>
/// Named authorization policies for the AuthN service surface. <see cref="Public"/> is satisfied by
/// any principal, anonymous-role cookie included — Login/Register/Logout must still declare a policy
/// per decided law item 4, even though that policy imposes no real requirement. <see cref="Account"/>
/// is the self-service tier: a real, signed-in account, never the anonymous principal.
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

	/// <summary>
	///     Satisfied only by an authenticated principal that is not the anonymous principal — the
	///     self-service account tier (spec 2026-10-26-identity-account-lifecycle-over-grpc §4.4).
	/// </summary>
	public const string Account = "AuthN.Account";

	/// <summary>
	///     Configures <see cref="Account" />. The anonymous-cookie principal is <em>authenticated</em>
	///     (spec §4.4) — <c>RequireAuthenticatedUser()</c> alone admits it — so the
	///     <see cref="NorseRoles.Anonymous" /> role is excluded explicitly.
	/// </summary>
	/// <param name="policy">The builder to configure.</param>
	[NorsePolicy(Account)]
	public static void ConfigureAccount(AuthorizationPolicyBuilder policy) =>
		policy.RequireAuthenticatedUser().RequireAssertion(static context => !context.User.IsInRole(NorseRoles.Anonymous));
}
