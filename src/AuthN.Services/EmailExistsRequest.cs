using System.Runtime.Serialization;

namespace Norse.AuthN.Services;

/// <summary>
/// The wire request for the pre-submit email-existence check — UX sugar over an inherently racy
/// lookup; the atomic user-creation conflict in the register handler remains the authority.
/// </summary>
[DataContract]
public sealed record EmailExistsRequest
{
	/// <summary>The email address to check.</summary>
	[DataMember(Order = 1)]
	public required string Email { get; init; }
}
