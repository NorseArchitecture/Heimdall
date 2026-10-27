using System.Runtime.Serialization;

namespace Norse.AuthN.Services;

/// <summary>
///     The wire request for <see cref="IAccountService.RemoveExternalLogin" /> (spec §4.3). Both members
///     are opaque strings echoed from an <see cref="ExternalLoginEntry" />. A pure wire DTO — no
///     mediator marker, no <c>[Authorize]</c>.
/// </summary>
[DataContract]
public sealed record RemoveExternalLoginRequest
{
	/// <summary>The provider's scheme name.</summary>
	[DataMember(Order = 1)]
	public string LoginProvider { get; set; } = string.Empty;

	/// <summary>The provider's opaque key for the user.</summary>
	[DataMember(Order = 2)]
	public string ProviderKey { get; set; } = string.Empty;
}
