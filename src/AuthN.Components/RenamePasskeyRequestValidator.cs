using FluentValidation;
using Norse.AuthN.Services;

namespace Norse.AuthN.Components;

/// <summary>
///     Validator for <see cref="RenamePasskeyRequest" /> — the single source of truth for its validation rules on the
///     whole platform, same dual-run shape as <see cref="LoginRequestValidator" />: Blazor
///     Server/WASM run it client-side via Asgard's <c>FormValidator</c>; Himinbjörg's generated
///     <c>
///         CommandRequestValidator&lt;RenamePasskeyCommand,
///         RenamePasskeyRequest, NavigationResult&gt;
///     </c>
///     reaches through the server-sovereign <c>RenamePasskeyCommand</c>
///     wrapper and runs this exact class again server-side. One declaration, two consumers, never
///     duplicated.
///     <c>CredentialId</c> is an opaque base64url id echoed from a <see cref="PasskeyEntry" />; only its
///     presence is checked. <c>Name</c> is required and capped at 200 characters, the parity cap.
/// </summary>
public sealed class RenamePasskeyRequestValidator : AbstractValidator<RenamePasskeyRequest>
{
	/// <summary>Initializes a new instance of the <see cref="RenamePasskeyRequestValidator" /> class.</summary>
	public RenamePasskeyRequestValidator()
	{
		RuleFor(x => x.CredentialId)
			.NotEmpty();
		RuleFor(x => x.Name)
			.NotEmpty()
			.MaximumLength(200);
	}
}
