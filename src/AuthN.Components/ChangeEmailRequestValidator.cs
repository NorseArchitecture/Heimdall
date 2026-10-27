using FluentValidation;
using Norse.AuthN.Services;
using Norse.Primitives;
using Norse.Primitives.Pii;

namespace Norse.AuthN.Components;

/// <summary>
///     Validator for <see cref="ChangeEmailRequest" /> — the single source of truth for its validation rules on the
///     whole platform, same dual-run shape as <see cref="LoginRequestValidator" />: Blazor
///     Server/WASM run it client-side via Asgard's <c>FormValidator</c>; Himinbjörg's generated
///     <c>
///         CommandRequestValidator&lt;ChangeEmailCommand,
///         ChangeEmailRequest, BoolResponse&gt;
///     </c>
///     reaches through the server-sovereign <c>ChangeEmailCommand</c>
///     wrapper and runs this exact class again server-side. One declaration, two consumers, never
///     duplicated.
///     The email rule registers on the stamp (<c>NewEmail</c>, the <c>Result&lt;EmailAddress&gt;</c>),
///     never the raw buffer — the chain is copied from <see cref="LoginRequestValidator" />, whose
///     remarks explain why.
/// </summary>
public sealed class ChangeEmailRequestValidator : AbstractValidator<ChangeEmailRequest>
{
	/// <summary>Initializes a new instance of the <see cref="ChangeEmailRequestValidator" /> class.</summary>
	public ChangeEmailRequestValidator()
	{
		RuleFor(x => x.NewEmail)
			.Cascade(CascadeMode.Stop)
			.Must(email => !(email.TryGetValue(out Failure failure) && failure.Reason == ParseFailure.Empty))
			.WithMessage("Enter your new email address.")
			.WithName(nameof(ChangeEmailRequest.NewEmailInput))
			.Must(email => email.TryGetValue(out Success<EmailAddress> _))
			.WithMessage("Enter a valid email address (local@domain.tld).");
	}
}
