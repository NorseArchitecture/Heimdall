using System.Runtime.Serialization;

namespace Norse.AuthN.Services;

/// <summary>
///     The wire request for <see cref="IAccountService.SetPassword" /> (spec §4.3) — adds a password to
///     an account that has none. Deliberately mutable — the direct two-way <c>EditForm</c> binding
///     target. A pure wire DTO — no mediator marker, no <c>[Authorize]</c>.
/// </summary>
[DataContract]
public sealed record SetPasswordRequest
{
	/// <summary>The new password.</summary>
	[DataMember(Order = 1)]
	public string NewPassword { get; set; } = string.Empty;
}
