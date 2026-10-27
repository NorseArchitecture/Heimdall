using FluentValidation;
using Norse.AuthN.Services;
using Norse.Primitives;
using Norse.Primitives.Pii;

namespace Norse.AuthN.Components;

/// <summary>
///     Validator for <see cref="SetPhoneNumberRequest" /> — the single source of truth for its validation rules on the
///     whole platform, same dual-run shape as <see cref="LoginRequestValidator" />: Blazor
///     Server/WASM run it client-side via Asgard's <c>FormValidator</c>; Himinbjörg's generated
///     <c>
///         CommandRequestValidator&lt;SetPhoneNumberCommand,
///         SetPhoneNumberRequest, NavigationResult&gt;
///     </c>
///     reaches through the server-sovereign <c>SetPhoneNumberCommand</c>
///     wrapper and runs this exact class again server-side. One declaration, two consumers, never
///     duplicated.
///     The rule registers on the stamp (<c>Phone</c>, the <c>Result&lt;PhoneNumber&gt;?</c>), never the
///     raw buffer, with <c>WithName</c> carrying the buffer's name for display only — see
///     <see cref="LoginRequestValidator" />. A <see langword="null" /> stamp is the "clear the phone
///     number" signal and is accepted; only a <c>Success</c> is otherwise valid. Every <c>Failure</c> is
///     rejected — including <c>Empty</c>, which the buffer never stamps but a hand-made wire request
///     carrying a present-but-empty field deserializes to.
/// </summary>
public sealed class SetPhoneNumberRequestValidator : AbstractValidator<SetPhoneNumberRequest>
{
	/// <summary>Initializes a new instance of the <see cref="SetPhoneNumberRequestValidator" /> class.</summary>
	public SetPhoneNumberRequestValidator()
	{
		RuleFor(x => x.Phone)
			.Must(phone => phone is null || phone.Value.TryGetValue(out Success<PhoneNumber> _))
			.WithMessage("Enter a valid phone number (+15551234567).")
			.WithName(nameof(SetPhoneNumberRequest.PhoneInput));
	}
}
