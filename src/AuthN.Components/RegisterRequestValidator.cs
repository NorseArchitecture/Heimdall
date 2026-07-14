using FluentValidation;

namespace Norse.AuthN.Components;

/// <summary>Validator for <see cref="RegisterRequest"/>.</summary>
public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
	/// <summary>Initializes a new instance of the <see cref="RegisterRequestValidator"/> class.</summary>
	public RegisterRequestValidator()
	{
		RuleFor(x => x.Email).NotEmpty().EmailAddress();
		// Password *policy* specifics (breach lists, lockout backoff) are out of scope
		// (Heimdall/specs/2026-07-13-authn-identity-split-design.md carries this forward from
		// 2026-06-07-auth-design.md §2); NIST SP 800-63B's length-over-complexity floor is the only
		// rule enforced client/server-side here.
		RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
	}
}
