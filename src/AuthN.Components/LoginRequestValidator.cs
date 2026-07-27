using FluentValidation;
using Norse.AuthN.Services;

namespace Norse.AuthN.Components;

/// <summary>
/// Validator for <see cref="LoginRequest"/> — the single source of truth for login validation
/// rules on the whole platform. Blazor Server/WASM run it client-side via Blazilla's
/// <c>FluentValidator</c>; Himinbjörg's generated <c>CommandRequestValidator&lt;LoginCommand,
/// LoginRequest, LoginResult&gt;</c> reaches through the server-sovereign <c>LoginCommand</c>
/// wrapper and runs this exact class again server-side. One declaration, two consumers, never
/// duplicated.
/// </summary>
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
