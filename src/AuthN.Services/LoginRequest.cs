using System.Runtime.Serialization;

namespace Norse.AuthN.Services;

/// <summary>
/// Deliberately mutable (not <c>init</c>) — this is the direct two-way <c>EditForm</c> binding target
/// for <c>AuthN.Components.FluentUI</c>'s <c>Login.razor</c>; every other record in this contract stays
/// <c>init</c>-only. A pure wire DTO — no mediator marker, no <c>[Authorize]</c>. Himinbjörg's
/// server-sovereign <c>LoginCommand</c> wraps this instance to give it mediator identity; the wire
/// shape itself stays free of anything that would drag <c>Abstractions.Web.Server</c> into WASM's
/// footprint.
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
