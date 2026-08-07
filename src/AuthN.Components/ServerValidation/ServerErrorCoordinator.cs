using Microsoft.AspNetCore.Components.Forms;
using Norse.Abstractions.Contracts;

namespace Norse.AuthN.Components.ServerValidation;

/// <summary>
/// Owns the server-produced <see cref="ValidationMessageStore"/> for one <see cref="EditContext"/>,
/// plus the two subscriptions that make resubmission possible: field edits clear that field's server
/// messages, and any fresh validation pass clears them all. Without the second subscription a stale
/// server message keeps <see cref="EditContext.Validate"/> false forever and the valid-submit
/// handler can never run again — the live defect the hand-rolled components carried.
/// </summary>
sealed class ServerErrorCoordinator
{
	readonly EditContext _editContext;
	readonly ValidationMessageStore _messages;

	internal ServerErrorCoordinator(EditContext editContext)
	{
		_editContext = editContext;
		_messages = new(editContext);
		editContext.OnFieldChanged += (_, e) =>
		{
			_messages.Clear(e.FieldIdentifier);
			editContext.NotifyValidationStateChanged();
		};
		editContext.OnValidationRequested += (_, _) =>
		{
			// Notify after clearing: without it, UI cleanup would silently depend on some OTHER
			// store (Blazilla's) raising the notification afterward — a correctness dependency on
			// another library's implementation. A form with no other validator must still update.
			_messages.Clear();
			editContext.NotifyValidationStateChanged();
		};
	}

	internal void Apply(Problem problem)
	{
		_messages.Clear();
		if (problem.Errors.Count == 0)
			_messages.Add(new FieldIdentifier(_editContext.Model, string.Empty), CategoryDisplay.For(problem));
		else
			foreach (var (field, messages) in problem.Errors)
			{
				FieldIdentifier identifier = new(_editContext.Model, field);
				foreach (var message in messages)
					_messages.Add(identifier, message);
			}

		_editContext.NotifyValidationStateChanged();
	}

	internal void Clear()
	{
		_messages.Clear();
		_editContext.NotifyValidationStateChanged();
	}
}
