using System.Runtime.Serialization;

namespace Norse.AuthN.Services;

/// <summary>
///     The wire request for <see cref="IAccountService.ChangePassword" /> (spec §4.3). Deliberately
///     mutable — the direct two-way <c>EditForm</c> binding target. A pure wire DTO — no mediator
///     marker, no <c>[Authorize]</c>.
/// </summary>
[DataContract]
public sealed record ChangePasswordRequest
{
	/// <summary>The caller's current password.</summary>
	[DataMember(Order = 1)]
	public string OldPassword { get; set; } = string.Empty;

	/// <summary>The new password.</summary>
	[DataMember(Order = 2)]
	public string NewPassword { get; set; } = string.Empty;
}
