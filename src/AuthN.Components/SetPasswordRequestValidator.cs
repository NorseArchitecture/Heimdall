using FluentValidation;
using Norse.AuthN.Services;

namespace Norse.AuthN.Components;

/// <summary>
///     Validator for <see cref="SetPasswordRequest" /> — the single source of truth for its validation rules on the
///     whole platform, same dual-run shape as <see cref="LoginRequestValidator" />: Blazor
///     Server/WASM run it client-side via Asgard's <c>FormValidator</c>; Himinbjörg's generated
///     <c>
///         CommandRequestValidator&lt;SetPasswordCommand,
///         SetPasswordRequest, NavigationResult&gt;
///     </c>
///     reaches through the server-sovereign <c>SetPasswordCommand</c>
///     wrapper and runs this exact class again server-side. One declaration, two consumers, never
///     duplicated.
///     The password floor is 8, the platform's login floor.
/// </summary>
public sealed class SetPasswordRequestValidator : AbstractValidator<SetPasswordRequest>
{
	/// <summary>Initializes a new instance of the <see cref="SetPasswordRequestValidator" /> class.</summary>
	public SetPasswordRequestValidator()
	{
		RuleFor(x => x.NewPassword)
			.NotEmpty()
			.MinimumLength(8);
	}
}
