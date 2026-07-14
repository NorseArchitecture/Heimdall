using System.Runtime.Serialization;

namespace Norse.AuthN.Components;

/// <summary>
/// Deliberately mutable (not <c>init</c>) — this is the direct two-way <c>EditForm</c> binding target
/// for <c>AuthN.Components.FluentUI</c>'s <c>Login.razor</c>; every other record in this contract stays
/// <c>init</c>-only. Plain wire DTO — no mediator-law coupling of any kind (spec §9.1/§9.2).
/// </summary>
[DataContract]
public sealed record LoginRequest
{
	/// <summary>The user's email address.</summary>
	[DataMember(Order = 1)]
	public required string Email { get; set; }

	/// <summary>The user's password.</summary>
	[DataMember(Order = 2)]
	public required string Password { get; set; }

	/// <summary>Whether to persist the authentication cookie across browser sessions.</summary>
	[DataMember(Order = 3)]
	public bool RememberMe { get; set; }
}
