using System.Runtime.Serialization;
using Microsoft.AspNetCore.Authorization;
using Norse.Abstractions.Contracts;

namespace Norse.AuthN.Services;

/// <summary>
/// Deliberately mutable (not <c>init</c>) — this is the direct two-way <c>EditForm</c> binding target
/// for <c>AuthN.Components.FluentUI</c>'s <c>Login.razor</c>; every other record in this contract stays
/// <c>init</c>-only. Wire DTO marked as a command request — the marker couples it to
/// <c>Abstractions.Contracts</c> (WASM-safe, already referenced for <see cref="Outcome{T}"/>); the objection
/// raised 2026-07-24 was to server-only assemblies, which remain untouched.
/// </summary>
[DataContract]
[Authorize(Policy = AuthNPolicies.Public)]
public sealed record LoginRequest : ICommandRequest<BoolResponse>
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
