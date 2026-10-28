using System.Runtime.Serialization;

namespace Norse.AuthN.Services;

/// <summary>
///     The wire request for <see cref="ISignInMethodsService.PasskeySignIn" /> — the second half of the passkey
///     sign-in ceremony. A pure wire DTO — no mediator marker, no <c>[Authorize]</c>.
/// </summary>
[DataContract]
public sealed record PasskeySignInRequest
{
	/// <summary>The browser's assertion credential, as JSON.</summary>
	[DataMember(Order = 1)]
	public string CredentialJson { get; set; } = string.Empty;

	/// <summary>The opaque state from <see cref="PasskeyRequestOptionsResponse.AssertionState" />.</summary>
	[DataMember(Order = 2)]
	public string AssertionState { get; set; } = string.Empty;

	/// <summary>Whether to persist the authentication cookie across browser sessions.</summary>
	[DataMember(Order = 3)]
	public bool RememberMe { get; set; }

	/// <summary>
	///     The return URL the sign-in page received — the server validates it and authors the
	///     <c>NextUrl</c>; the client never navigates to it directly.
	/// </summary>
	[DataMember(Order = 4)]
	public string ReturnUrl { get; set; } = string.Empty;
}
