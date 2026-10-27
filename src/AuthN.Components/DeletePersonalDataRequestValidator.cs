using FluentValidation;
using Norse.AuthN.Services;

namespace Norse.AuthN.Components;

/// <summary>
///     Validator for <see cref="DeletePersonalDataRequest" /> — the single source of truth for its validation rules on the
///     whole platform, same dual-run shape as <see cref="LoginRequestValidator" />: Blazor
///     Server/WASM run it client-side via Asgard's <c>FormValidator</c>; Himinbjörg's generated
///     <c>
///         CommandRequestValidator&lt;DeletePersonalDataCommand,
///         DeletePersonalDataRequest, NavigationResult&gt;
///     </c>
///     reaches through the server-sovereign <c>DeletePersonalDataCommand</c>
///     wrapper and runs this exact class again server-side. One declaration, two consumers, never
///     duplicated.
///     Only a length ceiling is declared: whether the password is required depends on whether the
///     account has one, which only the server knows, so requiredness is the server's (§6.7).
/// </summary>
public sealed class DeletePersonalDataRequestValidator : AbstractValidator<DeletePersonalDataRequest>
{
	/// <summary>Initializes a new instance of the <see cref="DeletePersonalDataRequestValidator" /> class.</summary>
	public DeletePersonalDataRequestValidator()
	{
		RuleFor(x => x.Password)
			.MaximumLength(256);
	}
}
