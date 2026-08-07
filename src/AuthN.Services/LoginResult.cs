using System.Runtime.Serialization;
using Norse.Abstractions.Contracts;

namespace Norse.AuthN.Services;

/// <summary>
/// The wire response for <see cref="IAuthenticationService.Login"/>. A rejected login (wrong username
/// or password, deliberately never distinguished, see spec §9.3/§9.4) always carries the collapsed
/// <see cref="ErrorCategory.InvalidCredentials"/> problem on the <see cref="Failed"/> case instead —
/// this record is only ever seen on the <c>Success</c> side of the <see cref="Outcome{T}"/>. The former
/// <c>Succeeded</c> member predates the two-unions design — it was the pre-<see cref="Outcome{T}"/>/
/// <see cref="Problem"/> era's way of carrying failure on a success envelope; deleting it completes the
/// union migration rather than breaking a contract (ruled 2026-08-06).
/// </summary>
[DataContract]
public sealed record LoginResult
{
	/// <summary>
	/// The relative URL the client must navigate to next — always resolved server-side by Himinbjörg's
	/// <c>LoginHandler</c>, so every client (Blazor Server, WASM, MAUI) can call
	/// <c>Navigation.NavigateTo(result.NextUrl, forceLoad: true)</c> unconditionally: no flag to branch
	/// on, no default to fall back to, no route to construct. Three cases fold into this one field:
	/// <list type="bullet">
	/// <item>A second factor is still required — the password was correct (this is not the shared
	/// anti-enumeration <see cref="Failed"/> path, it rides success), but sign-in isn't complete — this
	/// is the full 2FA challenge page URL, remember-me choice included as a query parameter.</item>
	/// <item>Sign-in completed but the cookie write had to be deferred (the Blazor-Server in-process
	/// path: a circuit can't <c>Set-Cookie</c> once the response has started) — this is the
	/// forced-reload completion URL.</item>
	/// <item>Sign-in completed and the cookie was written directly (every other path) — this is
	/// simply <c>"/"</c>.</item>
	/// </list>
	/// </summary>
	[DataMember(Order = 2)]
	public required string NextUrl { get; init; }
}
