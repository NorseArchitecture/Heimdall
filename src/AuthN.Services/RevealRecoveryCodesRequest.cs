using System.Runtime.Serialization;

namespace Norse.AuthN.Services;

/// <summary>
///     The wire request for <see cref="ITwoFactorService.RevealRecoveryCodes" />. A pure wire DTO — no mediator marker, no <c>[Authorize]</c>.
/// </summary>
[DataContract]
public sealed record RevealRecoveryCodesRequest
{
	/// <summary>
	///     The opaque, one-shot recovery-code handoff the server put on the <c>NextUrl</c> of a
	///     stamp-rotating operation — the client never decodes it.
	/// </summary>
	[DataMember(Order = 1)]
	public string Handoff { get; set; } = string.Empty;
}
