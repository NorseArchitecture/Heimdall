using System.Runtime.Serialization;

namespace Norse.AuthN.Services;

/// <summary>
///     The wire request for <see cref="IPasskeyService.AddPasskey" /> — the second half of the registration
///     ceremony. A pure wire DTO — no mediator marker, no <c>[Authorize]</c>.
/// </summary>
[DataContract]
public sealed record AddPasskeyRequest
{
	/// <summary>The browser's attestation credential, as JSON.</summary>
	[DataMember(Order = 1)]
	public string CredentialJson { get; set; } = string.Empty;

	/// <summary>The opaque state from <see cref="PasskeyCreationOptionsResponse.AttestationState" />.</summary>
	[DataMember(Order = 2)]
	public string AttestationState { get; set; } = string.Empty;
}
