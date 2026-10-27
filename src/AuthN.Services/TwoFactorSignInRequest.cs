using System.Runtime.Serialization;

namespace Norse.AuthN.Services;

/// <summary>
///     Second-factor sign-in with an authenticator code — replaces the scaffold's
///     <c>LoginWith2fa.razor</c>. Deliberately mutable — see <see cref="LoginRequest" />'s remark. A
///     pure wire DTO, same as <see cref="LoginRequest" />.
/// </summary>
[DataContract]
public sealed record TwoFactorSignInRequest
{
	/// <summary>
	///     The opaque two-factor challenge token <c>Login</c> handed back in its <c>NextUrl</c> — the
	///     client never decodes it; remember-me rides inside it.
	/// </summary>
	[DataMember(Order = 1)]
	public string Challenge { get; set; } = string.Empty;

	/// <summary>
	///     The authenticator code, raw as typed — spaces and hyphens are stripped server-side, never
	///     here.
	/// </summary>
	[DataMember(Order = 2)]
	public string Code { get; set; } = string.Empty;

	/// <summary>Whether to remember this machine so the second factor is skipped next time.</summary>
	[DataMember(Order = 3)]
	public bool RememberMachine { get; set; }
}
