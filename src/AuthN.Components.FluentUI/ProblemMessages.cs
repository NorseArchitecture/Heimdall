using Norse.Abstractions.Contracts;

namespace Norse.AuthN.Components.FluentUI;

/// <summary>
///     The lines a page renders for a failed dispatch: the server's own messages whenever the
///     <see cref="Problem" /> carries any, otherwise the page's fallback — a gRPC-decoded fault arrives
///     without errors, and a failure must never render as an empty alert.
/// </summary>
static class ProblemMessages
{
	/// <summary>The failure's messages in order, or <paramref name="fallback" /> alone when it carries none.</summary>
	/// <param name="problem">The failure to render.</param>
	/// <param name="fallback">The page-authored line for a failure without messages.</param>
	/// <returns>The lines to render, never empty.</returns>
	internal static IReadOnlyList<string> Of(Problem problem, string fallback)
	{
		List<string> messages = [.. problem.Errors.Values.SelectMany(m => m)];
		return messages.Count > 0 ? messages : [fallback];
	}
}
