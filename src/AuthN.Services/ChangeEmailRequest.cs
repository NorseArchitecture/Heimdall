using System.Runtime.Serialization;
using Norse.Primitives;
using Norse.Primitives.Pii;

namespace Norse.AuthN.Services;

/// <summary>
///     The wire request for <see cref="IAccountService.ChangeEmail" /> (spec §4.3). Deliberately
///     mutable — the direct two-way <c>EditForm</c> binding target. A pure wire DTO — no mediator
///     marker, no <c>[Authorize]</c>.
/// </summary>
[DataContract]
public sealed record ChangeEmailRequest
{
	/// <summary>
	///     The new email address as a wire-stamped scalar — the serialized member (spec
	///     2026-08-08-wire-stamped-request-scalars). Deserialization is the parse event; the server
	///     holds its own verdict regardless of what the client claimed.
	/// </summary>
	[DataMember(Order = 1)]
	public Result<EmailAddress> NewEmail { get; set; }

	/// <summary>
	///     The form's raw buffer — never serialized; assignment stamps <see cref="NewEmail" />, so there
	///     is no code path that sets the text without refreshing the verdict. Named stamp +
	///     <c>Input</c> so the field-change echo maps buffer edits onto the stamp's rules.
	/// </summary>
	public required string NewEmailInput
	{
		get;
		set
		{
			field = value;
			NewEmail = EmailAddress.Parse(value);
		}
	} = string.Empty;
}
