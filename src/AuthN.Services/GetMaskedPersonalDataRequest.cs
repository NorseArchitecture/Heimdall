using System.Runtime.Serialization;

namespace Norse.AuthN.Services;

/// <summary>
/// The wire request for <see cref="IIdentityService.GetMaskedPersonalDataAsync"/>. A pure wire
/// DTO — no mediator marker, no <c>[Authorize]</c>; Himinbjörg's server-sovereign command wrapper
/// gives it mediator identity and enforces <see cref="IdentityPolicies.MaskedDisclosure"/>.
/// <c>init</c>-only, unlike <see cref="LoginRequest"/>'s deliberate mutability — no component in
/// this repo two-way binds this record against an <c>EditForm</c> (there is no masked-disclosure
/// page here yet), so it follows <see cref="LoginRequest"/>'s own remark that every record without
/// that specific need stays <c>init</c>-only.
/// </summary>
[DataContract]
public sealed record GetMaskedPersonalDataRequest
{
	/// <summary>The subject whose masked personal data is being requested.</summary>
	[DataMember(Order = 1)]
	public required Guid SubjectId { get; init; }
}
