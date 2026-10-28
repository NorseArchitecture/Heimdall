using System.Runtime.Serialization;

namespace Norse.AuthN.Services;

/// <summary>
///     The wire request for <see cref="IPasskeyService.GetPasskey" />. A pure wire DTO — no mediator marker, no <c>[Authorize]</c>.
/// </summary>
[DataContract]
public sealed record GetPasskeyRequest
{
	/// <summary>The base64url credential id, echoed from a <see cref="PasskeyEntry" />.</summary>
	[DataMember(Order = 1)]
	public string CredentialId { get; set; } = string.Empty;
}
