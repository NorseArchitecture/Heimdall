using FluentValidation;
using Norse.AuthN.Services;

namespace Norse.AuthN.Components;

/// <summary>
///     Validator for <see cref="RevealRecoveryCodesRequest" /> — the single source of truth for its validation rules on the
///     whole platform, same dual-run shape as <see cref="LoginRequestValidator" />: Blazor
///     Server/WASM run it client-side via Asgard's <c>FormValidator</c>; Himinbjörg's generated
///     <c>
///         CommandRequestValidator&lt;TCommand,
///         RevealRecoveryCodesRequest, RecoveryCodesResponse&gt;
///     </c>
///     reaches through the server-sovereign command
///     wrapper and runs this exact class again server-side. One declaration, two consumers, never
///     duplicated.
///     <c>Handoff</c> is opaque and one-shot — the client never decodes it; only its presence is checked.
/// </summary>
public sealed class RevealRecoveryCodesRequestValidator : AbstractValidator<RevealRecoveryCodesRequest>
{
	/// <summary>Initializes a new instance of the <see cref="RevealRecoveryCodesRequestValidator" /> class.</summary>
	public RevealRecoveryCodesRequestValidator()
	{
		RuleFor(x => x.Handoff)
			.NotEmpty();
	}
}
