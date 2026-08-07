using System.Runtime.Serialization;
using Norse.Abstractions.Contracts;

namespace Norse.AuthN.Services;

/// <summary>
/// The wire response for <see cref="IAuthenticationService.Login"/>. A bare success signal — a
/// rejected login (wrong username or password, deliberately never distinguished, see spec §9.3/§9.4)
/// always carries the collapsed <see cref="ErrorCategory.InvalidCredentials"/> problem on the
/// <see cref="Failed"/> case instead. The former <c>Succeeded</c> member
/// predates the two-unions design — it was the pre-<see cref="Outcome{T}"/>/<see cref="Problem"/> era's
/// way of carrying failure on a success envelope; deleting it completes the union migration rather
/// than breaking a contract (ruled 2026-08-06).
/// </summary>
[DataContract]
public sealed record LoginResult
{
	/// <summary>
	/// Non-null only on the Blazor-Server in-process path, when the sign-in had to be deferred to a
	/// forced-reload completion request (spec: circuits can't Set-Cookie once the response has
	/// started). Always null for real gRPC/WASM calls — that path never stashes a deferred sign-in.
	/// </summary>
	[DataMember(Order = 2)]
	public string? DeferredCompletionUrl { get; init; }
}
