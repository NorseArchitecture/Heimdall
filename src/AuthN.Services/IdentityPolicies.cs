namespace Norse.AuthN.Services;

/// <summary>
/// Named authorization policies for the identity disclosure surface (<see cref="IIdentityService"/>).
/// Constants only — this assembly never references <c>Abstractions.Web.Server</c>, so the actual
/// <c>RequireRole</c>/policy registration is Himinbjörg's job, server-side (Task 19b). These names
/// are wire-adjacent metadata the concrete host mirrors onto its methods for gRPC endpoint
/// discovery, the same role <see cref="AuthNPolicies.Public"/> plays for the issuance surface.
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
