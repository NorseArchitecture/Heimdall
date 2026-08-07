using Microsoft.AspNetCore.Components.Forms;
using Norse.Abstractions.Components;
using Norse.Abstractions.Contracts;
using Norse.AuthN.Components.ServerValidation;
using Norse.Primitives;

namespace Norse.AuthN.Components;

/// <summary>
/// The pit-of-success submit seam: pages hand <see cref="SubmitAsync{T}(EditContext, Func{CancellationToken, Task{Outcome{T}}}, Action{T})"/>
/// the call and the success continuation, and the <see cref="Outcome{T}"/> error story is handled where it
/// cannot be forgotten — <c>Failed</c> renders through <see cref="EditContextServerErrorsExtensions.ApplyServerErrors"/>,
/// success clears prior server errors before the continuation runs. Total over the <see cref="Outcome{T}"/> domain
/// only: exceptions (a throwing transport, a throwing continuation) propagate to the circuit's error boundary
/// deliberately — swallowing them here would be a silent fallback.
/// </summary>
public abstract class OutcomeFormComponentBase : AsyncComponentBase
{
	/// <summary>True while a submit is in flight — bind to the submit button's <c>Disabled</c> state.</summary>
	protected bool IsSubmitting { get; private set; }

	/// <summary>Synchronous-continuation convenience over the <see cref="Func{T, Task}"/> overload.</summary>
	protected Task SubmitAsync<T>(EditContext editContext, Func<CancellationToken, Task<Outcome<T>>> call, Action<T> onSuccess)
		where T : notnull =>
		SubmitAsync(editContext, call, value =>
		{
			onSuccess(value);
			return Task.CompletedTask;
		});

	/// <summary>Dispatches <paramref name="call"/> and routes its <see cref="Outcome{T}"/>: failure into the form, success into <paramref name="onSuccess"/>.</summary>
	protected async Task SubmitAsync<T>(EditContext editContext, Func<CancellationToken, Task<Outcome<T>>> call, Func<T, Task> onSuccess)
		where T : notnull
	{
		ArgumentNullException.ThrowIfNull(editContext);
		if (IsSubmitting)
			return;

		IsSubmitting = true;
		try
		{
			// CA2007 deliberately suppressed, not worked around: component code must resume on the
			// renderer's sync context, so ConfigureAwait(false) here would be a correctness bug, not
			// a style nit. See the class remarks.
#pragma warning disable CA2007
			var outcome = await call(CancellationToken);
			switch (outcome)
			{
				case Success<T>(var value):
					editContext.ClearServerErrors();
					await onSuccess(value);
					break;
				case Failed(var problem):
					editContext.ApplyServerErrors(problem);
					break;
			}
#pragma warning restore CA2007
		}
		finally
		{
			IsSubmitting = false;
		}
	}
}
