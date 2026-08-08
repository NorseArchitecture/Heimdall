using FluentValidation;
using Microsoft.Extensions.Logging;
using Norse.Abstractions.Contracts;
using Norse.AuthN.Services;
using Norse.Primitives;
using Norse.Primitives.Pii;

namespace Norse.AuthN.Components;

/// <summary>
/// Validator for <see cref="RegisterRequest"/> — the single source of truth for registration
/// validation rules on the whole platform. Blazor Server/WASM run it client-side via Blazilla's
/// <c>FluentValidator</c>; Himinbjörg's generated <c>CommandRequestValidator&lt;RegisterCommand,
/// RegisterRequest, RegisterResult&gt;</c> reaches through the server-sovereign <c>RegisterCommand</c>
/// wrapper and runs this exact class again server-side. One declaration, two consumers, never
/// duplicated.
///
/// The email rule lives entirely in FluentValidation's default rule set — there is no rule-set
/// gating to a "submit" pass (ruled 2026-08-06, spec §6.1 amendment): Blazilla's field-change pass
/// builds a bare <c>MemberNameValidatorSelector</c> that carries no rule-set guard, so gating the
/// async lookup to a submit-only set is structurally unbuildable against this platform's actual
/// Blazilla/FluentValidation versions. Instead the whole email chain is one
/// <c>Cascade(CascadeMode.Stop)</c> rule: the sync shape check runs first, and the async existence
/// lookup only fires once the shape is already sound — client-side that's the email field's change
/// event (blur, not keystroke, since <c>FluentTextInput</c> binds on change), server-side it's the
/// unmodified <c>CommandRequestValidator</c> adapter running the same chain.
/// </summary>
public sealed partial class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="RegisterRequestValidator"/> class. Not a C# 12
	/// primary constructor: FluentValidation's <c>RuleFor</c> chain has to run as constructor-body
	/// statements, and a class with a primary constructor has nowhere to put them — the platform's
	/// own <c>CustomAsync</c> write-back proof (<c>AddressValidator</c>,
	/// <c>CustomAsyncWriteBackTests.cs</c>) uses the identical ordinary-constructor shape for the
	/// same reason. DI resolves this constructor exactly as it would a primary one.
	/// </summary>
	/// <param name="authenticationService">The gRPC contract this validator calls to check email availability.</param>
	/// <param name="logger">The logger for lookup failures.</param>
	public RegisterRequestValidator(IAuthenticationService authenticationService, ILogger<RegisterRequestValidator> logger)
	{
		RuleFor(x => x.Email)
			.Cascade(CascadeMode.Stop)
			.Must(email => !(email.TryGetValue(out Failure failure) && failure.Reason == ParseFailure.Empty))
			.WithMessage("Enter your email address.")
			.WithName(nameof(RegisterRequest.EmailInput))
			.Must(email => email.TryGetValue(out Success<EmailAddress> _))
			.WithMessage("Enter a valid email address (local@domain.tld).")
			.CustomAsync(async (email, context, cancellationToken) =>
			{
				// The rule's property IS the stamp, so it arrives proven here (the success gate sits
				// ahead under Cascade(Stop)) and passes through verbatim — no re-parse, no second
				// format authority; the validity gate doubles as the traffic filter, so unproven
				// input never buys this round trip or its database query.
				var outcome = await authenticationService.EmailExists(new() { Email = email }, cancellationToken).ConfigureAwait(false);
				switch (outcome)
				{
					case Success<BoolResponse>({ Value: true }):
						context.AddFailure("This email is already registered.");
						break;
					case Success<BoolResponse>:
						break;
					case Failed(var problem):
						LogEmailExistsLookupFailed(logger, problem.Category, problem.CorrelationId);
						context.AddFailure("Could not verify this email right now — try again.");
						break;
				}
			});
		// Password *policy* specifics (breach lists, lockout backoff) are out of scope
		// (Heimdall/specs/2026-07-13-authn-identity-split-design.md carries this forward from
		// 2026-06-07-auth-design.md §2); NIST SP 800-63B's length-over-complexity floor is the only
		// rule enforced client/server-side here.
		RuleFor(x => x.Password)
			.NotEmpty()
			.MinimumLength(8);
	}

	[LoggerMessage(Level = LogLevel.Error, Message = "Email-exists lookup failed: {Category} (correlation {CorrelationId})")]
	static partial void LogEmailExistsLookupFailed(ILogger logger, ErrorCategory category, Guid? correlationId);
}
