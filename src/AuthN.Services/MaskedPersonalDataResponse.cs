using System.Runtime.Serialization;

namespace Norse.AuthN.Services;

/// <summary>
/// The masked wire response for <see cref="IIdentityService.GetMaskedPersonalDataAsync"/> —
/// second-party disclosure, gated by <see cref="IdentityPolicies.MaskedDisclosure"/>. The same two
/// members as <see cref="PersonalDataResponse"/>, always masked at the source — the endpoint
/// chooses masked, it never authors a mask (spec §6).
/// </summary>
[DataContract]
public sealed record MaskedPersonalDataResponse
{
	/// <summary>The subject's email address, masked.</summary>
	[DataMember(Order = 1)]
	public required string Email { get; init; }

	/// <summary>The subject's phone number, masked. Empty string when the subject has none on file — never null.</summary>
	[DataMember(Order = 2)]
	public required string PhoneNumber { get; init; }
}
