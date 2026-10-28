using FluentValidation;
using Norse.AuthN.Services;

namespace Norse.AuthN.Components;

/// <summary>
///     Validator for <see cref="RemovePasskeyRequest" /> — the single source of truth for its validation rules on the
///     whole platform, same dual-run shape as <see cref="LoginRequestValidator" />: Blazor
///     Server/WASM run it client-side via Asgard's <c>FormValidator</c>; Himinbjörg's generated
///     <c>
///         CommandRequestValidator&lt;RemovePasskeyCommand,
///         RemovePasskeyRequest, BoolResponse&gt;
///     </c>
///     reaches through the server-sovereign <c>RemovePasskeyCommand</c>
///     wrapper and runs this exact class again server-side. One declaration, two consumers, never
///     duplicated.
///     <c>CredentialId</c> is an opaque base64url id echoed from a <see cref="PasskeyEntry" />; only its
///     presence is checked.
/// </summary>
public sealed class RemovePasskeyRequestValidator : AbstractValidator<RemovePasskeyRequest>
{
	/// <summary>Initializes a new instance of the <see cref="RemovePasskeyRequestValidator" /> class.</summary>
	public RemovePasskeyRequestValidator()
	{
		RuleFor(x => x.CredentialId)
			.NotEmpty();
	}
}
