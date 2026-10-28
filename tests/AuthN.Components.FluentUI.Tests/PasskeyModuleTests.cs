using Bunit;
using Microsoft.JSInterop;

namespace Norse.AuthN.Components.FluentUI.Tests;

// The wrapper's own contract, pinned below the page: Blazor's renderer swallows a canceled event-handler
// task, so a page-level fact could not tell an abort from an escaped cancellation.
public sealed class PasskeyModuleTests : BunitContext
{
	const string OptionsJson = """{"challenge":"c"}""";

	[Fact]
	async Task A_ceremony_canceled_by_the_callers_token_is_an_abort()
	{
		var module = JSInterop.SetupModule(PasskeyModule.Path);
		var ceremony = module.Setup<PasskeyCeremonyResult>("request", OptionsJson, "optional");
		await using PasskeyModule passkeys = new(JSInterop.JSRuntime);
		using CancellationTokenSource cancellation = new();

		var pending = passkeys.RequestAsync(OptionsJson, "optional", cancellation.Token);
		await cancellation.CancelAsync();
		ceremony.SetCanceled();

		(await pending).ShouldBe(new(null, null));
	}

	[Fact]
	async Task A_js_exception_in_a_ceremony_is_its_error_line()
	{
		var module = JSInterop.SetupModule(PasskeyModule.Path);
		module.Setup<PasskeyCeremonyResult>("create", OptionsJson).SetException(new JSException("The operation is insecure."));
		await using PasskeyModule passkeys = new(JSInterop.JSRuntime);

		(await passkeys.CreateAsync(OptionsJson, Xunit.TestContext.Current.CancellationToken))
			.ShouldBe(new(null, "The operation is insecure."));
	}

	[Fact]
	async Task A_dropped_circuit_during_the_probe_answers_unsupported()
	{
		var module = JSInterop.SetupModule(PasskeyModule.Path);
		module.Setup<bool>("isSupported").SetException(new JSDisconnectedException("The circuit is gone."));
		await using PasskeyModule passkeys = new(JSInterop.JSRuntime);

		(await passkeys.IsSupportedAsync(Xunit.TestContext.Current.CancellationToken)).ShouldBeFalse();
	}

	[Fact]
	async Task A_failed_import_on_the_probe_propagates()
	{
		JSInterop.Mode = JSRuntimeMode.Strict;
		await using PasskeyModule passkeys = new(JSInterop.JSRuntime);

		// Strict mode answers an unplanned import with its own exception; the probe lets it through rather than
		// reading it as "unsupported". The page renders it in place (PasskeysTests).
		await Should.ThrowAsync<Exception>(() => passkeys.IsSupportedAsync(Xunit.TestContext.Current.CancellationToken));
	}
}
