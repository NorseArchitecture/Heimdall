using System.Runtime.Serialization;

namespace Norse.AuthN.Services;

/// <summary>
///     Second-factor sign-in with a one-time recovery code — replaces the scaffold's
///     <c>LoginWithRecoveryCode.razor</c>. Never persistent (scaffold parity). Deliberately mutable —
///     see <see cref="LoginRequest" />'s remark. A pure wire DTO, same as <see cref="LoginRequest" />.
/// </summary>
[DataContract]
public sealed record RecoveryCodeSignInRequest
{
	/// <summary>The opaque two-factor challenge token <c>Login</c> handed back; never decoded client-side.</summary>
	[DataMember(Order = 1)]
	public string Challenge { get; set; } = string.Empty;

	/// <summary>The one-time recovery code, opaque to the client.</summary>
	[DataMember(Order = 2)]
	public string RecoveryCode { get; set; } = string.Empty;
}
