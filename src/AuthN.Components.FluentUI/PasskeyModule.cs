using Microsoft.JSInterop;

namespace Norse.AuthN.Components.FluentUI;

/// <summary>
///     The one door to <c>wwwroot/passkeys.js</c>: it owns the module import (lazy, once), the support
///     probe, the two ceremonies, the narrow <see cref="JSException" /> catch, and disposal. A page constructs
///     one over its injected <see cref="IJSRuntime" /> after its first interactive render (a static render has
///     no browser to ask) and disposes it from its own <c>DisposeAsync</c>. The module never fetches: the
///     page hands it the server's options JSON and hands the server the credential JSON it answers.
/// </summary>
/// <param name="jsRuntime">The page's JS runtime.</param>
sealed class PasskeyModule(IJSRuntime jsRuntime) : IAsyncDisposable
{
	/// <summary>The module's static-asset path.</summary>
	internal const string Path = "./_content/Norse.AuthN.Components.FluentUI/passkeys.js";

	IJSObjectReference? _module;

	/// <summary>
	///     Whether the browser can run a passkey ceremony (<c>parseCreationOptionsFromJSON</c> and
	///     <c>parseRequestOptionsFromJSON</c> both exist). A failed import propagates: it is not an answer.
	///     Only a dropped circuit is caught, and it answers false, since there is no page left to offer Add on.
	/// </summary>
	/// <param name="cancellationToken">The page's disposal token.</param>
	/// <returns>True when both ceremonies are available.</returns>
	internal async Task<bool> IsSupportedAsync(CancellationToken cancellationToken = default)
	{
		try
		{
			_module ??= await jsRuntime.InvokeAsync<IJSObjectReference>("import", cancellationToken, Path).ConfigureAwait(false);
			return await _module.InvokeAsync<bool>("isSupported", cancellationToken).ConfigureAwait(false);
		}
		catch (JSDisconnectedException)
		{
			return false;
		}
	}

	/// <summary>The registration ceremony over the server's creation options (<c>navigator.credentials.create</c>).</summary>
	/// <param name="optionsJson">The server's WebAuthn creation options, as JSON.</param>
	/// <param name="cancellationToken">The page's disposal token.</param>
	/// <returns>The ceremony's credential, error, or abort.</returns>
	internal Task<PasskeyCeremonyResult> CreateAsync(string optionsJson, CancellationToken cancellationToken = default) =>
		CeremonyAsync("create", [optionsJson], cancellationToken);

	/// <summary>The assertion ceremony over the server's request options (<c>navigator.credentials.get</c>).</summary>
	/// <param name="optionsJson">The server's WebAuthn request options, as JSON.</param>
	/// <param name="mediation">The credential-management mediation, for example <c>"optional"</c>.</param>
	/// <param name="cancellationToken">The page's disposal token.</param>
	/// <returns>The ceremony's credential, error, or abort.</returns>
	internal Task<PasskeyCeremonyResult> RequestAsync(string optionsJson, string mediation, CancellationToken cancellationToken = default) =>
		CeremonyAsync("request", [optionsJson, mediation], cancellationToken);

	/// <summary>
	///     Runs one ceremony. The module maps the browser's own failures (a dismissed prompt, an abort);
	///     a <see cref="JSException" /> is what remains (an interop or script failure), and it becomes the
	///     error line, rendered in place. Cancellation by the caller's own token is an abort. Nothing broader
	///     is caught.
	/// </summary>
	async Task<PasskeyCeremonyResult> CeremonyAsync(string identifier, object?[] args, CancellationToken cancellationToken)
	{
		try
		{
			_module ??= await jsRuntime.InvokeAsync<IJSObjectReference>("import", cancellationToken, Path).ConfigureAwait(false);
			return await _module.InvokeAsync<PasskeyCeremonyResult>(identifier, cancellationToken, args).ConfigureAwait(false);
		}
		catch (JSException exception)
		{
			return new(null, exception.Message);
		}
		// The caller's own token: the page is gone, so the ceremony is an abort, nothing to render or send.
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			return new(null, null);
		}
	}

	/// <summary>Releases the module reference, if it was ever imported. A dropped circuit has nothing left to release.</summary>
	public async ValueTask DisposeAsync()
	{
		if (_module is null)
			return;
		try
		{
			await _module.DisposeAsync().ConfigureAwait(false);
		}
		catch (JSDisconnectedException)
		{
		}
	}
}
