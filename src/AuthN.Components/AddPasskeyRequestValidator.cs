using FluentValidation;
using Norse.AuthN.Services;

namespace Norse.AuthN.Components;

/// <summary>
///     Validator for <see cref="AddPasskeyRequest" /> — the single source of truth for its validation rules on the
///     whole platform, same dual-run shape as <see cref="LoginRequestValidator" />: Blazor
///     Server/WASM run it client-side via Asgard's <c>FormValidator</c>; Himinbjörg's generated
///     <c>
///         CommandRequestValidator&lt;AddPasskeyCommand,
///         AddPasskeyRequest, NavigationResult&gt;
///     </c>
///     reaches through the server-sovereign <c>AddPasskeyCommand</c>
///     wrapper and runs this exact class again server-side. One declaration, two consumers, never
///     duplicated.
///     Both members are opaque — the browser's attestation JSON and the server's attestation state;
///     only their presence is checked. The ceremony itself is verified server-side.
/// </summary>
public sealed class AddPasskeyRequestValidator : AbstractValidator<AddPasskeyRequest>
{
	/// <summary>Initializes a new instance of the <see cref="AddPasskeyRequestValidator" /> class.</summary>
	public AddPasskeyRequestValidator()
	{
		RuleFor(x => x.CredentialJson)
			.NotEmpty();
		RuleFor(x => x.AttestationState)
			.NotEmpty();
	}
}
