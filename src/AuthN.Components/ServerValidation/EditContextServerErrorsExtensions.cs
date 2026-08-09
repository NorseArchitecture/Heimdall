using Microsoft.AspNetCore.Components.Forms;
using Norse.Abstractions.Contracts;

namespace Norse.AuthN.Components.ServerValidation;

/// <summary>
///     Applies and clears server-produced validation errors on an <see cref="EditContext" />. Backed by
///     a <see cref="ServerErrorCoordinator" /> cached once per context, so resubmission works correctly:
///     a fresh validation pass clears every server message instead of leaving
///     <see cref="EditContext.Validate" /> permanently false.
/// </summary>
public static class EditContextServerErrorsExtensions
{
	/// <summary>
	///     The <see cref="EditContext.Properties" /> key the cached <see cref="ServerErrorCoordinator" />
	///     is stored under. Internal so the platform's own tests can assert caching without exposing the
	///     coordinator type as public API.
	/// </summary>
	internal static readonly object CoordinatorKey = new();

	extension(EditContext editContext)
	{
		/// <summary>
		///     Renders <paramref name="problem" /> against <paramref name="editContext" />: field-keyed
		///     messages from <see cref="Problem.Errors" /> when present, otherwise a category-appropriate
		///     sentence at the model level. Replaces any previously applied server errors rather than
		///     accumulating them.
		/// </summary>
		/// <param name="problem">The server-produced problem to render.</param>
		public void ApplyServerErrors(Problem problem)
		{
			ArgumentNullException.ThrowIfNull(editContext);
			ArgumentNullException.ThrowIfNull(problem);

			CoordinatorFor(editContext).Apply(problem);
		}

		/// <summary>Removes every server-produced validation message from <paramref name="editContext" />.</summary>
		public void ClearServerErrors()
		{
			ArgumentNullException.ThrowIfNull(editContext);

			CoordinatorFor(editContext).Clear();
		}
	}

	static ServerErrorCoordinator CoordinatorFor(EditContext editContext)
	{
		if (editContext.Properties.TryGetValue(CoordinatorKey, out var existing))
			return (ServerErrorCoordinator)existing;

		ServerErrorCoordinator coordinator = new(editContext);
		editContext.Properties[CoordinatorKey] = coordinator;
		return coordinator;
	}
}
