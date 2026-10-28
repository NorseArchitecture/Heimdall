using FluentValidation;
using Norse.AuthN.Services;

namespace Norse.AuthN.Components;

/// <summary>
///     Validator for <see cref="GetPasskeyRequest" /> — the single source of truth for its validation rules on the
///     whole platform, same dual-run shape as <see cref="LoginRequestValidator" />: Blazor
///     Server/WASM run it client-side via Asgard's <c>FormValidator</c>; Himinbjörg's generated
///     <c>
///         CommandRequestValidator&lt;TCommand,
///         GetPasskeyRequest, PasskeyEntry&gt;
///     </c>
///     reaches through the server-sovereign command
///     wrapper and runs this exact class again server-side. One declaration, two consumers, never
///     duplicated.
///     <c>CredentialId</c> is an opaque base64url id echoed from a <see cref="PasskeyEntry" />; only its
///     presence is checked.
/// </summary>
public sealed class GetPasskeyRequestValidator : AbstractValidator<GetPasskeyRequest>
{
	/// <summary>Initializes a new instance of the <see cref="GetPasskeyRequestValidator" /> class.</summary>
	public GetPasskeyRequestValidator()
	{
		RuleFor(x => x.CredentialId)
			.NotEmpty();
	}
}
