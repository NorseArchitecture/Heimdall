using System.Runtime.Serialization;

namespace Norse.AuthN.Services;

/// <summary>
///     Confirms an account's email address from the emailed link — replaces the scaffold's
///     <c>ConfirmEmail.razor</c>. Deliberately mutable — see <see cref="LoginRequest" />'s remark. A
///     pure wire DTO, same as <see cref="LoginRequest" />.
/// </summary>
[DataContract]
public sealed record ConfirmEmailRequest
{
	/// <summary>The opaque user identifier from the emailed link.</summary>
	[DataMember(Order = 1)]
	public string UserId { get; set; } = string.Empty;

	/// <summary>The opaque confirmation code from the emailed link; never decoded client-side.</summary>
	[DataMember(Order = 2)]
	public string Code { get; set; } = string.Empty;
}
