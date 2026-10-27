using System.Runtime.Serialization;
using Norse.Primitives;
using Norse.Primitives.Pii;

namespace Norse.AuthN.Services;

/// <summary>
///     Confirms a pending email change from the emailed link — replaces the scaffold's
///     <c>ConfirmEmailChange.razor</c>. Email and username change together (law). Deliberately
///     mutable — see <see cref="LoginRequest" />'s remark. A pure wire DTO, same as
///     <see cref="LoginRequest" />.
/// </summary>
[DataContract]
public sealed record ConfirmEmailChangeRequest
{
	/// <summary>The opaque user identifier from the emailed link.</summary>
	[DataMember(Order = 1)]
	public string UserId { get; set; } = string.Empty;

	/// <summary>
	///     The new email address, as a wire-stamped scalar — the serialized member. Non-nullable, so the field is
	///     required: the forge mints the verdict, the request declares the obligation (spec
	///     2026-08-08-wire-stamped-request-scalars). Deserialization is the parse event; the server
	///     holds its own verdict regardless of what the client claimed.
	/// </summary>
	[DataMember(Order = 2)]
	public Result<EmailAddress> Email { get; set; }

	/// <summary>
	///     The form's raw buffer — never serialized; assignment stamps <see cref="Email" />, so
	///     there is no code path that sets the text without refreshing the verdict. A client-side
	///     artifact: the sanctioned deserialization path never assigns it, so on the server it
	///     legitimately holds its empty default.
	/// </summary>
	public required string EmailInput
	{
		get;
		set
		{
			field = value;
			Email = EmailAddress.Parse(value);
		}
	} = string.Empty;

	/// <summary>The opaque change code from the emailed link; never decoded client-side.</summary>
	[DataMember(Order = 3)]
	public string Code { get; set; } = string.Empty;
}
