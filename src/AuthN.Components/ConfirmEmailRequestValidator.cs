using FluentValidation;
using Norse.AuthN.Services;

namespace Norse.AuthN.Components;

/// <summary>
///     Validator for <see cref="ConfirmEmailRequest" /> — the single source of truth for its validation rules on the
///     whole platform, same dual-run shape as <see cref="LoginRequestValidator" />: Blazor
///     Server/WASM run it client-side via Asgard's <c>FormValidator</c>; Himinbjörg's generated
///     <c>
///         CommandRequestValidator&lt;ConfirmEmailCommand,
///         ConfirmEmailRequest, BoolResponse&gt;
///     </c>
///     reaches through the server-sovereign <c>ConfirmEmailCommand</c>
///     wrapper and runs this exact class again server-side. One declaration, two consumers, never
///     duplicated.
/// </summary>
public sealed class ConfirmEmailRequestValidator : AbstractValidator<ConfirmEmailRequest>
{
	/// <summary>Initializes a new instance of the <see cref="ConfirmEmailRequestValidator" /> class.</summary>
	public ConfirmEmailRequestValidator()
	{
		RuleFor(x => x.UserId)
			.NotEmpty();
		RuleFor(x => x.Code)
			.NotEmpty();
	}
}
