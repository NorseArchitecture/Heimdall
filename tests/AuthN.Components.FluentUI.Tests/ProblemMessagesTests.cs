using Norse.Abstractions.Contracts;

namespace Norse.AuthN.Components.FluentUI.Tests;

public sealed class ProblemMessagesTests
{
	[Fact]
	void The_problems_messages_win_in_order()
	{
		Problem problem = new()
		{
			Category = ErrorCategory.Validation,
			Errors = new Dictionary<string, string[]> { ["A"] = ["first", "second"], [""] = ["third"] },
		};

		ProblemMessages.Of(problem, "fallback").ShouldBe(["first", "second", "third"]);
	}

	// A gRPC-decoded fault arrives without errors.
	[Fact]
	void A_problem_without_errors_yields_the_fallback()
	{
		ProblemMessages.Of(new() { Category = ErrorCategory.Fault }, "fallback").ShouldBe(["fallback"]);
	}
}
