using FluentValidation;
using Norse.AuthN.Services;

namespace Norse.AuthN.Components;

/// <summary>Validator for <see cref="LoginRequest"/>.</summary>
public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
	/// <summary>Initializes a new instance of the <see cref="LoginRequestValidator"/> class.</summary>
	public LoginRequestValidator()
	{
		RuleFor(x => x.Email)
			.NotEmpty()
			.EmailAddress();
		RuleFor(x => x.Password)
			.NotEmpty()
			.MinimumLength(8);
	}
}
