using System.Runtime.Serialization;
using Norse.Primitives;
using Norse.Primitives.Pii;

namespace Norse.AuthN.Services;

/// <summary>Deliberately mutable — see <see cref="LoginRequest" />'s remark. A pure wire DTO, same as <see cref="LoginRequest" />.</summary>
[DataContract]
public sealed record RegisterRequest
{
	/// <summary>
	///     The email address for the new account, as a wire-stamped scalar — the serialized member.
	///     Non-nullable, so the field is required: the forge mints the verdict, the request declares
	///     the obligation (spec 2026-08-08-wire-stamped-request-scalars). Deserialization is the
	///     parse event; the server holds its own verdict regardless of what the client claimed.
	/// </summary>
	[DataMember(Order = 1)]
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

	/// <summary>The password for the new account.</summary>
	[DataMember(Order = 2)]
	public string Password { get; set; } = string.Empty;
}
