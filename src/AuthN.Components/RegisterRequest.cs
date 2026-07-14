using System.Runtime.Serialization;

namespace Norse.AuthN.Components;

/// <summary>Deliberately mutable — see <see cref="LoginRequest"/>'s remark.</summary>
[DataContract]
public sealed record RegisterRequest
{
	/// <summary>The email address for the new account.</summary>
	[DataMember(Order = 1)]
	public required string Email { get; set; }

	/// <summary>The password for the new account.</summary>
	[DataMember(Order = 2)]
	public required string Password { get; set; }
}
