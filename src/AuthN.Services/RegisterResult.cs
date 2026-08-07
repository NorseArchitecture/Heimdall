using System.Runtime.Serialization;

namespace Norse.AuthN.Services;

/// <summary>
/// The wire response for <see cref="IAuthenticationService.Register"/>. A bare success signal — a
/// failed registration always carries field errors on the <see cref="Norse.Abstractions.Contracts.Failed"/>
/// case instead (Conflict for a taken email, Validation for a rejected password), so there is no
/// anti-enumeration collapse to preserve here the way <see cref="LoginResult"/>'s collapsed
/// <see cref="Norse.Abstractions.Contracts.ErrorCategory.InvalidCredentials"/> problem preserves one
/// for sign-in.
/// </summary>
[DataContract]
public sealed record RegisterResult
{
	/// <summary>Whether the account was created.</summary>
	[DataMember(Order = 1)]
	public required bool Succeeded { get; init; }
}
