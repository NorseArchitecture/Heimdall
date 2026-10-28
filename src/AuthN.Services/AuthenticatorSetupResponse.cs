using System.Runtime.Serialization;

namespace Norse.AuthN.Services;

/// <summary>
///     The wire response for <see cref="ITwoFactorService.GetAuthenticatorSetup" /> — what the enable-
///     authenticator page renders in place. A missing key is provisioned server-side without rotating
///     the security stamp; an unverified key is not a factor until <see cref="ITwoFactorService.EnableAuthenticator" />.
/// </summary>
[DataContract]
public sealed record AuthenticatorSetupResponse
{
	/// <summary>The authenticator key, formatted for manual entry.</summary>
	[DataMember(Order = 1)]
	public string SharedKey { get; set; } = string.Empty;

	/// <summary>The <c>otpauth://</c> URI the QR code encodes.</summary>
	[DataMember(Order = 2)]
	public string AuthenticatorUri { get; set; } = string.Empty;

	/// <summary>
	///     The QR code, rendered server-side as SVG. The client shows it through an
	///     <c>&lt;img src="data:image/svg+xml;base64,…"&gt;</c>, never as markup — an image cannot run script.
	/// </summary>
	[DataMember(Order = 3)]
	public string QrCodeSvg { get; set; } = string.Empty;
}
