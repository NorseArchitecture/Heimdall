using System.Runtime.Serialization;

namespace Norse.AuthN.Services;

/// <summary>
/// The wire response for <see cref="IAuthenticationService.Logout"/>. Clearing the auth cookie hits
/// the same <c>Response.HasStarted</c> constraint as setting one (spec: circuits can't Set-Cookie
/// once the response has started), so sign-out needs the identical deferred-completion mechanism as
/// <see cref="LoginResult"/> — a bare success/failure signal alone would silently regress the
/// already-proven sign-out deferral path.
/// </summary>
[DataContract]
public sealed record LogoutResult
{
	/// <summary>
	/// Non-null only on the Blazor-Server in-process path, when the sign-out had to be deferred to a
	/// forced-reload completion request. Always null for real gRPC/WASM calls — that path never
	/// stashes a deferred sign-out.
	/// </summary>
	[DataMember(Order = 1)]
	public string? DeferredCompletionUrl { get; init; }
}
