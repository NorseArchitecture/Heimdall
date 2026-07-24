using System.Runtime.Serialization;

namespace Norse.AuthN.Services;

/// <summary>
/// The wire response for <see cref="IAuthenticationService.Login"/>. <c>Succeeded=false</c> is a
/// legitimate successful credential check (wrong username or password), not a failure — the two are
/// deliberately never distinguished, see spec §9.3/§9.4.
/// </summary>
[DataContract]
public sealed record LoginResult
{
	/// <summary>Whether the login attempt succeeded.</summary>
	[DataMember(Order = 1)]
	public required bool Succeeded { get; init; }
}
