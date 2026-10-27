using System.Runtime.Serialization;

namespace Norse.AuthN.Services;

/// <summary>
///     The wire request for <see cref="IAccountService.DeletePersonalData" /> (spec §4.3, §5.6).
///     Deliberately mutable — the direct two-way <c>EditForm</c> binding target. A pure wire DTO — no
///     mediator marker, no <c>[Authorize]</c>.
/// </summary>
[DataContract]
public sealed record DeletePersonalDataRequest
{
	/// <summary>
	///     The caller's password. Always on the wire; the client renders the field only when
	///     <see cref="AccountStatusResponse.HasPassword" />, and the server checks it only then.
	/// </summary>
	[DataMember(Order = 1)]
	public string Password { get; set; } = string.Empty;
}
