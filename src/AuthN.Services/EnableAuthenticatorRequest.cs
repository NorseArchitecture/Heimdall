using System.Runtime.Serialization;

namespace Norse.AuthN.Services;

/// <summary>
///     The wire request for <see cref="ITwoFactorService.EnableAuthenticator" />. Deliberately mutable — see
///     <see cref="LoginRequest" />'s remark. A pure wire DTO — no mediator marker, no <c>[Authorize]</c>.
/// </summary>
[DataContract]
public sealed record EnableAuthenticatorRequest
{
	/// <summary>
	///     The authenticator code, raw as typed — spaces and hyphens are stripped server-side, never
	///     here.
	/// </summary>
	[DataMember(Order = 1)]
	public string Code { get; set; } = string.Empty;
}
