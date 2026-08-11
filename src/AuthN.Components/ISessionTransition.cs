using Norse.Abstractions.Contracts;

namespace Norse.AuthN.Components;

/// <summary>
///     The principal changed — re-establish this interactive session at the server-resolved next hop.
///     Components performing a principal transition (sign-in, sign-out) request the transition here
///     instead of touching <see cref="Microsoft.AspNetCore.Components.NavigationManager" />; the host
///     decides what stands behind it. Realm law, not platform law: only the gate changes who the user
///     is, so only the gate declares — and implements — the seam. The contract has no domain failure
///     arm; exceptional failures propagate to the circuit's error boundary.
/// </summary>
public interface ISessionTransition
{
	/// <summary>Begins the transition. Completion, if any, is the next document load's concern.</summary>
	void Begin(NavigationResult result);
}
