using Norse.Abstractions.Contracts;

namespace Norse.AuthN.Components.ServerValidation;

/// <summary>
/// Renders a generic, category-appropriate sentence for a <see cref="Problem"/> that carries no
/// field-keyed messages — a model-level failure with nothing more specific to say (e.g.
/// <see cref="ErrorCategory.Forbidden"/>, <see cref="ErrorCategory.Fault"/>).
/// </summary>
static class CategoryDisplay
{
	/// <summary>
	/// Returns the display sentence for <paramref name="problem"/>'s category, appending a
	/// correlation reference when one is present — always for <see cref="ErrorCategory.Fault"/>,
	/// which never reaches a user without a trace handle to hand a support agent.
	/// </summary>
	/// <param name="problem">The problem to render a sentence for.</param>
	internal static string For(Problem problem)
	{
		var sentence = problem.Category switch
		{
			ErrorCategory.Validation => "The submitted data isn't valid.",
			ErrorCategory.NotFound => "The requested resource couldn't be found.",
			ErrorCategory.Conflict => "This conflicts with existing data.",
			ErrorCategory.LockedOut => "This account is locked out. Try again later.",
			ErrorCategory.InvalidCredentials => "Invalid email or password.",
			ErrorCategory.NotAllowed => "This operation isn't allowed right now.",
			ErrorCategory.Unauthorized => "Sign in to continue.",
			ErrorCategory.Forbidden => "You don't have permission to do this.",
			ErrorCategory.MultipleMatches => "More than one match was found.",
			ErrorCategory.Erased => "This record no longer exists.",
			_ => "Something went wrong.",
		};

		return problem.CorrelationId is { } correlationId
			? $"{sentence} Reference: {correlationId}"
			: sentence;
	}
}
