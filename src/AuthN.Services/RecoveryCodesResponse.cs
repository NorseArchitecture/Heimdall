using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Norse.AuthN.Services;

/// <summary>
///     The wire response carrying freshly issued recovery codes — <see cref="ITwoFactorService.RevealRecoveryCodes" />
///     and <see cref="ITwoFactorService.GenerateRecoveryCodes" />. Absent codes are <c>[]</c>, never null.
/// </summary>
[DataContract]
public sealed record RecoveryCodesResponse
{
	/// <summary>The recovery codes, each redeemable once.</summary>
	[DataMember(Order = 1)]
	[SuppressMessage("Design", "CA1819:Properties should not return arrays",
		Justification =
			"An init-only [DataContract] wire record: the array is the serialized repeated field, replaced whole on construction and never mutated in place — not a public collection-design surface.")]
	public string[] RecoveryCodes { get; init; } = [];
}
