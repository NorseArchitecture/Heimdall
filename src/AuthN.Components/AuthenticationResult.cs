using System.Runtime.Serialization;
using Norse.AuthN.Services;

namespace Norse.AuthN.Components;

/// <summary>
/// The only thing any Razor component reads — never <see cref="IAuthenticationService"/> directly,
/// never a caught exception. Produced by a host-specific gateway (Blazor Server or WASM), per spec
/// §9.6. <c>Errors</c> convention: field name key -&gt; field-level messages; empty string key ("") -&gt;
/// general/model-level messages (e.g. account locked out) — matches FluentValidation/Blazor's own
/// convention for a message not tied to a specific property, so both flow into the same
/// ValidationSummary/ValidationMessageStore with no special-casing in the UI.
/// </summary>
[DataContract]
public sealed record AuthenticationResult
{
	/// <summary>Whether the authentication attempt succeeded.</summary>
	[DataMember(Order = 1)]
	public required bool Succeeded { get; init; }

	/// <summary>A dictionary of validation errors keyed by property name; empty string key is reserved for model-level errors.</summary>
	[DataMember(Order = 2)]
	public IReadOnlyDictionary<string, string[]> Errors { get; init; } = new Dictionary<string, string[]>();

	/// <summary>
	/// Non-null only when sign-in/out couldn't complete on this request (an already-established Blazor
	/// Server interactive circuit, where the response has already started) and must be completed via a
	/// forced full-page navigation instead — the component should <c>NavigationManager.NavigateTo</c> this
	/// URL with <c>forceLoad: true</c>. WASM/MAUI gateways never set this: their calls are always real,
	/// distinct HTTP requests, so sign-in/out always completes immediately.
	/// </summary>
	[DataMember(Order = 3)]
	public string? DeferredCompletionUrl { get; init; }
}
