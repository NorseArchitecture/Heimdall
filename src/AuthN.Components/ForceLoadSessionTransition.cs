using Microsoft.AspNetCore.Components;
using Norse.Abstractions.Contracts;

namespace Norse.AuthN.Components;

/// <summary>
///     The production <see cref="ISessionTransition" />: a real document load, so the circuit (or WASM
///     runtime) that holds the stale principal is torn down and re-established under the new identity.
///     Named for its mechanism — contracts name the role, implementations name what distinguishes them.
///     The one call site NORSE074 absolves — matched by this exact type name AND this assembly, so the
///     exemption is unforgeable; even the gate's own pages are convicted if they force a load directly.
/// </summary>
sealed class ForceLoadSessionTransition(NavigationManager navigation) : ISessionTransition
{
	public void Begin(NavigationResult result)
	{
		ArgumentNullException.ThrowIfNull(result);
		navigation.NavigateTo(result.NextUrl, forceLoad: true);
	}
}
