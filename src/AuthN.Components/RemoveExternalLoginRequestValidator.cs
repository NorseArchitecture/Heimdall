using FluentValidation;
using Norse.AuthN.Services;

namespace Norse.AuthN.Components;

/// <summary>
///     Validator for <see cref="RemoveExternalLoginRequest" /> — the single source of truth for its validation rules on the
///     whole platform, same dual-run shape as <see cref="LoginRequestValidator" />: Blazor
///     Server/WASM run it client-side via Asgard's <c>FormValidator</c>; Himinbjörg's generated
///     <c>
///         CommandRequestValidator&lt;RemoveExternalLoginCommand,
///         RemoveExternalLoginRequest, NavigationResult&gt;
///     </c>
///     reaches through the server-sovereign <c>RemoveExternalLoginCommand</c>
///     wrapper and runs this exact class again server-side. One declaration, two consumers, never
///     duplicated.
///     Both members are opaque strings echoed from an <see cref="ExternalLoginEntry" />; only their
///     presence is checked.
/// </summary>
public sealed class RemoveExternalLoginRequestValidator : AbstractValidator<RemoveExternalLoginRequest>
{
	/// <summary>Initializes a new instance of the <see cref="RemoveExternalLoginRequestValidator" /> class.</summary>
	public RemoveExternalLoginRequestValidator()
	{
		RuleFor(x => x.LoginProvider)
			.NotEmpty();
		RuleFor(x => x.ProviderKey)
			.NotEmpty();
	}
}
