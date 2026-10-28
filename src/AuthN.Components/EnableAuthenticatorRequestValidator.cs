using FluentValidation;
using Norse.AuthN.Services;

namespace Norse.AuthN.Components;

/// <summary>
///     Validator for <see cref="EnableAuthenticatorRequest" /> — the single source of truth for its validation rules on the
///     whole platform, same dual-run shape as <see cref="LoginRequestValidator" />: Blazor
///     Server/WASM run it client-side via Asgard's <c>FormValidator</c>; Himinbjörg's generated
///     <c>
///         CommandRequestValidator&lt;EnableAuthenticatorCommand,
///         EnableAuthenticatorRequest, NavigationResult&gt;
///     </c>
///     reaches through the server-sovereign <c>EnableAuthenticatorCommand</c>
///     wrapper and runs this exact class again server-side. One declaration, two consumers, never
///     duplicated.
///     <c>Code</c> is checked raw — 6 to 7 characters, the same rule as
///     <see cref="TwoFactorSignInRequestValidator" /> — so <c>123 456</c> and <c>123-456</c> pass; spaces and
///     hyphens are stripped server-side only.
/// </summary>
public sealed class EnableAuthenticatorRequestValidator : AbstractValidator<EnableAuthenticatorRequest>
{
	/// <summary>Initializes a new instance of the <see cref="EnableAuthenticatorRequestValidator" /> class.</summary>
	public EnableAuthenticatorRequestValidator()
	{
		RuleFor(x => x.Code)
			.NotEmpty()
			.Length(6, 7);
	}
}
