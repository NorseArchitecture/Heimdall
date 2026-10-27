using FluentValidation;
using Norse.AuthN.Services;

namespace Norse.AuthN.Components;

/// <summary>
///     Validator for <see cref="ChangePasswordRequest" /> — the single source of truth for its validation rules on the
///     whole platform, same dual-run shape as <see cref="LoginRequestValidator" />: Blazor
///     Server/WASM run it client-side via Asgard's <c>FormValidator</c>; Himinbjörg's generated
///     <c>
///         CommandRequestValidator&lt;ChangePasswordCommand,
///         ChangePasswordRequest, NavigationResult&gt;
///     </c>
///     reaches through the server-sovereign <c>ChangePasswordCommand</c>
///     wrapper and runs this exact class again server-side. One declaration, two consumers, never
///     duplicated.
///     The password floor is 8, the platform's login floor, and the new password must differ from
///     the current one. The new-password chain stops at its first failure, so an empty form never also
///     claims the two passwords must differ.
/// </summary>
public sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
	/// <summary>Initializes a new instance of the <see cref="ChangePasswordRequestValidator" /> class.</summary>
	public ChangePasswordRequestValidator()
	{
		RuleFor(x => x.OldPassword)
			.NotEmpty();
		RuleFor(x => x.NewPassword)
			.Cascade(CascadeMode.Stop)
			.NotEmpty()
			.MinimumLength(8)
			.NotEqual(x => x.OldPassword)
			.WithMessage("Your new password must differ from your current password.");
	}
}
