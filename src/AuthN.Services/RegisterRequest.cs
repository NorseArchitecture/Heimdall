using System.Runtime.Serialization;
using Microsoft.AspNetCore.Authorization;
using Norse.Abstractions.Contracts;

namespace Norse.AuthN.Services;

/// <summary>Deliberately mutable — see <see cref="LoginRequest"/>'s remark.</summary>
[DataContract]
[Authorize(Policy = AuthNPolicies.Public)]
public sealed record RegisterRequest : ICommandRequest<BoolResponse>
{
	/// <summary>The email address for the new account.</summary>
	[DataMember(Order = 1)]
	public required string Email { get; set; }

	/// <summary>The password for the new account.</summary>
	[DataMember(Order = 2)]
	public required string Password { get; set; }
}
