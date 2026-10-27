using FluentValidation;
using Norse.AuthN.Services;

namespace Norse.AuthN.Components;

/// <summary>
///     Validator for <see cref="RecoveryCodeSignInRequest" /> — the single source of truth for its validation rules on the
///     whole platform, same dual-run shape as <see cref="LoginRequestValidator" />: Blazor
///     Server/WASM run it client-side via Asgard's <c>FormValidator</c>; Himinbjörg's generated
///     <c>
///         CommandRequestValidator&lt;RecoveryCodeSignInCommand,
///         RecoveryCodeSignInRequest, NavigationResult&gt;
///     </c>
///     reaches through the server-sovereign <c>RecoveryCodeSignInCommand</c>
///     wrapper and runs this exact class again server-side. One declaration, two consumers, never
///     duplicated.
/// </summary>
public sealed class RecoveryCodeSignInRequestValidator : AbstractValidator<RecoveryCodeSignInRequest>
{
	/// <summary>Initializes a new instance of the <see cref="RecoveryCodeSignInRequestValidator" /> class.</summary>
	public RecoveryCodeSignInRequestValidator()
	{
		RuleFor(x => x.Challenge)
			.NotEmpty();
		RuleFor(x => x.RecoveryCode)
			.NotEmpty();
	}
}
