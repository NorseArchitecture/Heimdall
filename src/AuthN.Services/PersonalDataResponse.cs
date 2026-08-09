using System.Runtime.Serialization;

namespace Norse.AuthN.Services;

/// <summary>
///     The full, unmasked wire response for <see cref="IIdentityService.GetMyPersonalDataAsync" /> —
///     self-disclosure only, gated by <see cref="IdentityPolicies.Self" />. Both members are canonical
///     wire strings, never the underlying PII primitive itself (spec §1.5).
/// </summary>
[DataContract]
public sealed record PersonalDataResponse
{
	/// <summary>The subject's email address, unmasked.</summary>
	[DataMember(Order = 1)]
	public required string Email { get; init; }

	/// <summary>The subject's phone number, unmasked. Empty string when the subject has none on file — never null.</summary>
	[DataMember(Order = 2)]
	public required string PhoneNumber { get; init; }
}
