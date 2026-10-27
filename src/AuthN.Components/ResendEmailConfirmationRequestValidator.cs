using FluentValidation;
using Norse.AuthN.Services;
using Norse.Primitives;
using Norse.Primitives.Pii;

namespace Norse.AuthN.Components;

/// <summary>
///     Validator for <see cref="ResendEmailConfirmationRequest" /> — the single source of truth for its validation rules on the
///     whole platform, same dual-run shape as <see cref="LoginRequestValidator" />: Blazor
///     Server/WASM run it client-side via Asgard's <c>FormValidator</c>; Himinbjörg's generated
///     <c>
///         CommandRequestValidator&lt;ResendEmailConfirmationCommand,
///         ResendEmailConfirmationRequest, BoolResponse&gt;
///     </c>
///     reaches through the server-sovereign <c>ResendEmailConfirmationCommand</c>
///     wrapper and runs this exact class again server-side. One declaration, two consumers, never
///     duplicated.
///     The email rule registers on the stamp (<c>Email</c>, the <c>Result&lt;EmailAddress&gt;</c>), never
///     the raw buffer — the chain is copied from <see cref="LoginRequestValidator" />, whose remarks
///     explain why.
/// </summary>
public sealed class ResendEmailConfirmationRequestValidator : AbstractValidator<ResendEmailConfirmationRequest>
{
	/// <summary>Initializes a new instance of the <see cref="ResendEmailConfirmationRequestValidator" /> class.</summary>
	public ResendEmailConfirmationRequestValidator()
	{
		RuleFor(x => x.Email)
			.Cascade(CascadeMode.Stop)
			.Must(email => !(email.TryGetValue(out Failure failure) && failure.Reason == ParseFailure.Empty))
			.WithMessage("Enter your email address.")
			.WithName(nameof(ResendEmailConfirmationRequest.EmailInput))
			.Must(email => email.TryGetValue(out Success<EmailAddress> _))
			.WithMessage("Enter a valid email address (local@domain.tld).");
	}
}
