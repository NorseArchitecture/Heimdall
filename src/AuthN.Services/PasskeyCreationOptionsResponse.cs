using System.Runtime.Serialization;

namespace Norse.AuthN.Services;

/// <summary>
///     The wire response for <see cref="IPasskeyService.GetPasskeyCreationOptions" /> — the first half of the
///     registration ceremony, stateless on the server.
/// </summary>
[DataContract]
public sealed record PasskeyCreationOptionsResponse
{
	/// <summary>The WebAuthn creation options, as JSON for <c>navigator.credentials.create</c>.</summary>
	[DataMember(Order = 1)]
	public string OptionsJson { get; set; } = string.Empty;

	/// <summary>
	///     The data-protected ceremony state — opaque to the client, echoed back on
	///     <see cref="IPasskeyService.AddPasskey" />.
	/// </summary>
	[DataMember(Order = 2)]
	public string AttestationState { get; set; } = string.Empty;
}
