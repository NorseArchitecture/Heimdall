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

	/// <summary>
	///     The read-only account tier: what <see cref="Account" /> admits, plus a user bearer minted for
	///     <see cref="NorseAudiences.User" /> (spec 2026-10-29-openiddict-authorization-code-pkce-step-up §7.5).
	/// </summary>
	public const string AccountRead = "AuthN.AccountRead";

	/// <summary>
	///     Configures <see cref="AccountRead" />: the <see cref="NorseLanes.Identity" /> or <see cref="NorseLanes.User" />
	///     lane, never the anonymous principal, and a <see cref="NorseLanes.User" />-lane principal must carry
	///     <c>aud = </c><see cref="NorseAudiences.User" />.
	/// </summary>
	/// <param name="policy">The builder to configure.</param>
	[NorsePolicy(AccountRead)]
	public static void ConfigureAccountRead(AuthorizationPolicyBuilder policy) =>
		policy.ReadsOwnAccount();

	/// <param name="policy">The builder to configure.</param>
	extension(AuthorizationPolicyBuilder policy)
	{
		/// <summary>
		///     The shape <see cref="AccountRead" /> and <see cref="IdentityPolicies.Self" /> share: a signed-in account on
		///     the cookie lane or on a user bearer pinned to <see cref="NorseAudiences.User" />.
		/// </summary>
		/// <returns>The builder, for chaining.</returns>
		internal AuthorizationPolicyBuilder ReadsOwnAccount() =>
			policy.RequireLane(NorseLanes.Identity, NorseLanes.User)
				.RequireAuthenticatedUser()
				.RequireAssertion(static context => !context.User.IsInRole(NorseRoles.Anonymous))
				.RequireAudience(NorseAudiences.User, NorseLanes.User);
	}
}
