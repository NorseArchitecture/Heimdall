using System.Runtime.Serialization;

namespace Norse.AuthN.Services;

/// <summary>
/// The wire request for <see cref="IIdentityService.GetMaskedPersonalDataAsync"/>. A pure wire
/// DTO — no mediator marker, no <c>[Authorize]</c>; Himinbjörg's server-sovereign command wrapper
/// gives it mediator identity and enforces <see cref="IdentityPolicies.MaskedDisclosure"/>.
/// </summary>
[DataContract]
public sealed record GetMaskedPersonalDataRequest
{
	/// <summary>The subject whose masked personal data is being requested.</summary>
	[DataMember(Order = 1)]
	public required Guid SubjectId { get; set; }
}
