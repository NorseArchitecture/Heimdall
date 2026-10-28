using FluentValidation;
using Norse.AuthN.Services;

namespace Norse.AuthN.Components;

/// <summary>
///     Validator for <see cref="PasskeySignInRequest" /> — the single source of truth for its validation rules on the
///     whole platform, same dual-run shape as <see cref="LoginRequestValidator" />: Blazor
///     Server/WASM run it client-side via Asgard's <c>FormValidator</c>; Himinbjörg's generated
///     <c>
///         CommandRequestValidator&lt;PasskeySignInCommand,
///         PasskeySignInRequest, NavigationResult&gt;
///     </c>
///     reaches through the server-sovereign <c>PasskeySignInCommand</c>
///     wrapper and runs this exact class again server-side. One declaration, two consumers, never
///     duplicated.
///     Both ceremony members are opaque — the browser's assertion JSON and the server's assertion
///     state; only their presence is checked. <c>ReturnUrl</c> carries no rule: only
///     <c>ReturnUrls.Resolve</c> decides a landing, server-side.
/// </summary>
public sealed class PasskeySignInRequestValidator : AbstractValidator<PasskeySignInRequest>
{
	/// <summary>Initializes a new instance of the <see cref="PasskeySignInRequestValidator" /> class.</summary>
	public PasskeySignInRequestValidator()
	{
		RuleFor(x => x.CredentialJson)
			.NotEmpty();
		RuleFor(x => x.AssertionState)
			.NotEmpty();
	}
}
