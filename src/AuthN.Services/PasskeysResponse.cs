using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Norse.AuthN.Services;

/// <summary>
///     The wire response for <see cref="IPasskeyService.GetPasskeys" />. Absent passkeys are <c>[]</c>, never null.
/// </summary>
[DataContract]
public sealed record PasskeysResponse
{
	/// <summary>The caller's registered passkeys.</summary>
	[DataMember(Order = 1)]
	[SuppressMessage("Design", "CA1819:Properties should not return arrays",
		Justification =
			"An init-only [DataContract] wire record: the array is the serialized repeated field, replaced whole on construction and never mutated in place — not a public collection-design surface.")]
	public PasskeyEntry[] Passkeys { get; init; } = [];
}
