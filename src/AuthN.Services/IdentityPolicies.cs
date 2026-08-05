namespace Norse.AuthN.Services;

/// <summary>
/// Named authorization policies for the identity disclosure surface (<see cref="IIdentityService"/>).
/// Constants only — this assembly never references <c>Abstractions.Web.Server</c>, so the actual
/// <c>RequireRole</c>/policy registration happens at the consuming host's composition root, the same
/// place <see cref="AuthNPolicies.Public"/> registers today: Yggdrasil's <c>Hosting.Web.Server</c>
/// <c>Program.cs</c>, not Himinbjörg. Himinbjörg's command wrappers only <em>name</em> these policies
/// via <c>[Authorize(Policy = ...)]</c> — naming a policy and registering what satisfies it are
/// different jobs, and only the composition root does the latter.
/// </summary>
public static class IdentityPolicies
{
	/// <summary>Satisfied only by the authenticated principal disclosing their own row — decidable from the principal alone (spec §6.1).</summary>
	public const string Self = "Identity.Self";

	/// <summary>Satisfied by a principal holding <see cref="SystemRole"/> — masked, second-party disclosure only.</summary>
	public const string MaskedDisclosure = "Identity.MaskedDisclosure";

	/// <summary>The role name <see cref="MaskedDisclosure"/> requires.</summary>
	public const string SystemRole = "System";
}
