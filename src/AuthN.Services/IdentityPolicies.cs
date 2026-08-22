using Microsoft.AspNetCore.Authorization;
using Norse.Abstractions.Components.Authorization;

namespace Norse.AuthN.Services;

/// <summary>
///     Named authorization policies for the identity disclosure surface (<see cref="IIdentityService" />).
///     This assembly never references <c>Abstractions.Web.Server</c> — the mediator law assembly — so
///     the configure methods below are the contributor hook (<see cref="NorsePolicyAttribute" />, from
///     the client-bundle-safe <c>Abstractions.Components</c>): the actual policy registration is
///     discovered from metadata and composed at the consuming host's composition root, the same place
///     <see cref="AuthNPolicies.Public" /> registers today: Yggdrasil's <c>Hosting.Web.Server</c>
///     <c>Program.cs</c>, not Himinbjörg. Himinbjörg's command wrappers only <em>name</em> these policies
///     via <c>[Authorize(Policy = ...)]</c> — naming a policy and configuring what satisfies it are
///     different jobs, and only the contributor hook plus the composition root do the latter.
/// </summary>
public static class IdentityPolicies
{
	/// <summary>
	///     Satisfied only by the authenticated principal disclosing their own row — decidable from the principal alone
	///     (spec §6.1).
	/// </summary>
	public const string Self = "Identity.Self";

	/// <summary>Satisfied by a principal holding <see cref="SystemRole" /> — masked, second-party disclosure only.</summary>
	public const string MaskedDisclosure = "Identity.MaskedDisclosure";

	/// <summary>The role name <see cref="MaskedDisclosure" /> requires.</summary>
	public const string SystemRole = "System";

	/// <summary>Configures <see cref="Self" />.</summary>
	/// <param name="policy">The builder to configure.</param>
	[NorsePolicy(Self)]
	public static void ConfigureSelf(AuthorizationPolicyBuilder policy) =>
		policy.RequireAuthenticatedUser();

	/// <summary>Configures <see cref="MaskedDisclosure" />.</summary>
	/// <param name="policy">The builder to configure.</param>
	[NorsePolicy(MaskedDisclosure)]
	public static void ConfigureMaskedDisclosure(AuthorizationPolicyBuilder policy) =>
		policy.RequireRole(SystemRole);
}
