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

	/// <summary>
	/// Non-null only on the Blazor-Server in-process path, when the sign-in had to be deferred to a
	/// forced-reload completion request (spec: circuits can't Set-Cookie once the response has
	/// started). Always null for real gRPC/WASM calls — that path never stashes a deferred sign-in.
	/// </summary>
	[DataMember(Order = 2)]
	public string? DeferredCompletionUrl { get; init; }
}
