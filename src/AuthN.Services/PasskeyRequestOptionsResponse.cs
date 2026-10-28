using System.Runtime.Serialization;

namespace Norse.AuthN.Services;

/// <summary>
///     The wire response for <see cref="ISignInMethodsService.GetPasskeyRequestOptions" /> — the first half of
///     the passkey sign-in ceremony, stateless on the server.
/// </summary>
[DataContract]
public sealed record PasskeyRequestOptionsResponse
{
	/// <summary>The WebAuthn request options, as JSON for <c>navigator.credentials.get</c>.</summary>
	[DataMember(Order = 1)]
	public string OptionsJson { get; set; } = string.Empty;

	/// <summary>
	///     The data-protected, one-shot ceremony state — opaque to the client, echoed back on
	///     <see cref="ISignInMethodsService.PasskeySignIn" />.
	/// </summary>
	[DataMember(Order = 2)]
	public string AssertionState { get; set; } = string.Empty;
}
