using System.Runtime.Serialization;

namespace Norse.AuthN.Services;

/// <summary>
///     One of the caller's registered passkeys — an element of <see cref="PasskeysResponse" /> and the
///     response of <see cref="IPasskeyService.GetPasskey" />.
/// </summary>
[DataContract]
public sealed record PasskeyEntry
{
	/// <summary>The credential id, base64url-encoded — the passkey's identity on every passkey operation.</summary>
	[DataMember(Order = 1)]
	public string CredentialId { get; set; } = string.Empty;

	/// <summary>The passkey's display name; <c>""</c> when unnamed.</summary>
	[DataMember(Order = 2)]
	public string Name { get; set; } = string.Empty;

	/// <summary>The server-authored URL of this passkey's rename page — the client never composes it.</summary>
	[DataMember(Order = 3)]
	public string RenameUrl { get; set; } = string.Empty;
}
