using FluentValidation;
using Norse.AuthN.Services;
using Norse.Primitives;
using Norse.Primitives.Pii;

namespace Norse.AuthN.Components;

/// <summary>
///     Validator for <see cref="LoginRequest" /> — the single source of truth for login validation
///     rules on the whole platform. Blazor Server/WASM run it client-side via Asgard's
///     <c>FormValidator</c>; Himinbjörg's generated
///     <c>
///         CommandRequestValidator&lt;LoginCommand,
///         LoginRequest, NavigationResult&gt;
///     </c>
///     reaches through the server-sovereign <c>LoginCommand</c>
///     wrapper and runs this exact class again server-side. One declaration, two consumers, never
///     duplicated.
///     THE RULE REGISTERS ON THE STAMP — <see cref="LoginRequest.Email" />, the
///     <c>Result&lt;EmailAddress&gt;</c> — so every predicate reads the parsed verdict and any
///     future business rule works against the domain struct, never a raw string: the parser owns
///     format truth, the validator owns business truth, no rule exists twice (the former
///     <c>EmailAddress()</c> regex — a second format authority — is gone). <c>WithName</c> carries
///     the buffer's name for message display only; field-change selection on blur is the
///     <c>StampFieldBridge</c>'s job (the buffer's change is echoed as the stamp's), so
///     <c>PropertyName</c> — and therefore server error keys — stay <c>Email</c>, wire-stable.
/// </summary>
public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
	/// <summary>Initializes a new instance of the <see cref="LoginRequestValidator" /> class.</summary>
	public LoginRequestValidator()
	{
		RuleFor(x => x.Email)
			.Cascade(CascadeMode.Stop)
			.Must(email => !(email.TryGetValue(out Failure failure) && failure.Reason == ParseFailure.Empty))
			.WithMessage("Enter your email address.")
			.WithName(nameof(LoginRequest.EmailInput))
			.Must(email => email.TryGetValue(out Success<EmailAddress> _))
			.WithMessage("Enter a valid email address (local@domain.tld).");
		RuleFor(x => x.Password)
			.NotEmpty()
			.MinimumLength(8);
	}
}
