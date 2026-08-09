using Microsoft.AspNetCore.Components.Forms;
using Norse.Abstractions.Contracts;
using Norse.AuthN.Components.ServerValidation;
using Norse.Primitives;

namespace Norse.AuthN.Components.Tests;

public sealed class OutcomeFormComponentBaseTests
{
	[Fact]
	async Task Success_invokes_the_continuation_and_clears_server_errors()
	{
		using Harness harness = new();
		var context = harness.ContextFor(new object());
		context.ApplyServerErrors(Problem.ModelError(ErrorCategory.InvalidCredentials, "Stale."));
		var invoked = false;

		await harness.Submit(context, _ => Task.FromResult<Outcome<FakeResult>>(new Success<FakeResult>(new())),
			_ => invoked = true);

		invoked.ShouldBeTrue();
		context.GetValidationMessages().ShouldBeEmpty();
	}

	[Fact]
	async Task Failure_applies_the_problem_and_skips_the_continuation()
	{
		using Harness harness = new();
		var model = new object();
		var context = harness.ContextFor(model);
		var invoked = false;

		await harness.Submit(context,
			_ => Task.FromResult<Outcome<FakeResult>>(
				new Failed(Problem.ModelError(ErrorCategory.LockedOut, "Locked."))),
			_ => invoked = true);

		invoked.ShouldBeFalse();
		context.GetValidationMessages(new FieldIdentifier(model, string.Empty)).ShouldBe(["Locked."]);
	}

	[Fact]
	async Task A_foreign_edit_context_is_rejected_loudly()
	{
		// Model="..." binding creates its own EditContext, silently bypassing the stamped-request
		// blur mechanic — the guard makes that a loud failure instead of a quiet UX regression.
		using Harness harness = new();
		EditContext foreign = new(new object());

		await Should.ThrowAsync<InvalidOperationException>(
			harness.Submit(foreign, _ => Task.FromResult<Outcome<FakeResult>>(new Success<FakeResult>(new())),
				_ => { }));
	}

	[Fact]
	async Task An_overlapping_call_returns_without_dispatching()
	{
		using Harness harness = new();
		var context = harness.ContextFor(new object());
		TaskCompletionSource<Outcome<FakeResult>> pending = new();
		var calls = 0;

		var first = harness.Submit(context, _ =>
		{
			calls++;
			return pending.Task;
		}, _ => { });
		await harness.Submit(context, _ =>
		{
			calls++;
			return pending.Task;
		}, _ => { });

		calls.ShouldBe(1);
		harness.Submitting.ShouldBeTrue();
		pending.SetResult(new Success<FakeResult>(new()));
		await first;
		harness.Submitting.ShouldBeFalse();
	}

	[Fact]
	async Task A_throwing_continuation_propagates_and_releases_the_guard()
	{
		using Harness harness = new();
		var context = harness.ContextFor(new object());

		await Should.ThrowAsync<InvalidOperationException>(
			harness.Submit(context, _ => Task.FromResult<Outcome<FakeResult>>(new Success<FakeResult>(new())),
				_ => throw new InvalidOperationException("continuation bug")));

		harness.Submitting.ShouldBeFalse();
	}

	sealed record FakeResult;

	sealed class Harness : OutcomeFormComponentBase
	{
		internal bool Submitting =>
			IsSubmitting;

		internal Task Submit<T>(EditContext editContext, Func<CancellationToken, Task<Outcome<T>>> call,
			Action<T> onSuccess) where T : notnull =>
			SubmitAsync(editContext, call, onSuccess);

		internal EditContext ContextFor(object request) =>
			EditContextFor(request);
	}
}
