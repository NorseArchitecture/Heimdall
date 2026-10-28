using System.Runtime.Serialization;

namespace Norse.AuthN.Services;

/// <summary>
///     The wire request for <see cref="IPasskeyService.RenamePasskey" />. Deliberately mutable — see
///     <see cref="LoginRequest" />'s remark. A pure wire DTO — no mediator marker, no <c>[Authorize]</c>.
/// </summary>
[DataContract]
public sealed record RenamePasskeyRequest
{
	/// <summary>The base64url credential id, echoed from a <see cref="PasskeyEntry" />.</summary>
	[DataMember(Order = 1)]
	public string CredentialId { get; set; } = string.Empty;

	/// <summary>The passkey's new display name.</summary>
	[DataMember(Order = 2)]
	public string Name { get; set; } = string.Empty;
}
