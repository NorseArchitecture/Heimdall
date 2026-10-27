using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Norse.AuthN.Services;

/// <summary>
///     The wire response for <see cref="IAccountService.GetExternalLogins" /> (spec §4.3). Absent
///     collections are <c>[]</c>, never null.
/// </summary>
[DataContract]
public sealed record ExternalLoginsResponse
{
	/// <summary>The external logins linked to the caller's account.</summary>
	[DataMember(Order = 1)]
	[SuppressMessage("Design", "CA1819:Properties should not return arrays",
		Justification =
			"An init-only [DataContract] wire record: the array is the serialized repeated field, replaced whole on construction and never mutated in place — not a public collection-design surface.")]
	public ExternalLoginEntry[] Logins { get; init; } = [];

	/// <summary>The configured providers not yet linked to the caller's account.</summary>
	[DataMember(Order = 2)]
	[SuppressMessage("Design", "CA1819:Properties should not return arrays",
		Justification =
			"An init-only [DataContract] wire record: the array is the serialized repeated field, replaced whole on construction and never mutated in place — not a public collection-design surface.")]
	public ExternalProviderEntry[] AvailableProviders { get; init; } = [];

	/// <summary>
	///     Whether the page should offer a remove button — presentation, not the guard.
	///     <see cref="IAccountService.RemoveExternalLogin" /> is refused server-side regardless when it
	///     would remove the last usable sign-in method (§5.9).
	/// </summary>
	[DataMember(Order = 3)]
	public required bool CanRemove { get; init; }
}
