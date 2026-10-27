using System.Runtime.Serialization;
using Norse.Primitives;
using Norse.Primitives.Pii;

namespace Norse.AuthN.Services;

/// <summary>
///     The wire request for <see cref="IAccountService.SetPhoneNumber" /> (spec §4.3). Deliberately
///     mutable — the direct two-way <c>EditForm</c> binding target. A pure wire DTO — no mediator
///     marker, no <c>[Authorize]</c>.
/// </summary>
[DataContract]
public sealed record SetPhoneNumberRequest
{
	/// <summary>
	///     The phone number as a wire-stamped scalar — the serialized member (spec
	///     2026-08-08-wire-stamped-request-scalars). <see langword="null" /> is the "clear the phone number"
	///     signal (§4.3, <c>{ Phone? }</c>): an empty or whitespace buffer stamps null, and a null member is
	///     omitted from the wire. A non-empty malformed buffer stamps a <c>Failure</c>, which the validator
	///     rejects — a failed <see cref="Result{T}" /> is never written to the wire.
	/// </summary>
	[DataMember(Order = 1)]
	public Result<PhoneNumber>? Phone { get; set; }

	/// <summary>
	///     The form's raw buffer — never serialized; assignment stamps <see cref="Phone" />, so there is
	///     no code path that sets the text without refreshing the verdict. A client-side artifact: on
	///     the server it legitimately holds its empty default.
	/// </summary>
	public required string PhoneInput
	{
		get;
		set
		{
			field = value;
			Phone = string.IsNullOrWhiteSpace(value) ? null : PhoneNumber.Parse(value);
		}
	} = string.Empty;
}
