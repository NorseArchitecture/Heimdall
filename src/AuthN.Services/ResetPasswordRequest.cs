using System.Runtime.Serialization;
using Norse.Primitives;
using Norse.Primitives.Pii;

namespace Norse.AuthN.Services;

/// <summary>
///     Resets a password with the emailed reset code — replaces the scaffold's
///     <c>ResetPassword.razor</c>. Anti-enumeration contract: an unknown or erased user, a malformed
///     token, and an invalid token against a real account all answer one <c>InvalidCredentials</c>
///     ("The reset link is invalid or has expired."); only a genuine reset answers
///     <c>/Account/ResetPasswordConfirmation</c>. Deliberately mutable — see
///     <see cref="LoginRequest" />'s remark. A pure wire DTO, same as <see cref="LoginRequest" />.
/// </summary>
[DataContract]
public sealed record ResetPasswordRequest
{
	/// <summary>
	///     The account's email address, as a wire-stamped scalar — the serialized member. Non-nullable, so the field is
	///     required: the forge mints the verdict, the request declares the obligation (spec
	///     2026-08-08-wire-stamped-request-scalars). Deserialization is the parse event; the server
	///     holds its own verdict regardless of what the client claimed.
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

	/// <summary>The opaque reset code from the emailed link; never decoded client-side.</summary>
	[DataMember(Order = 2)]
	public string Code { get; set; } = string.Empty;

	/// <summary>The new password.</summary>
	[DataMember(Order = 3)]
	public string Password { get; set; } = string.Empty;
}
