using FluentValidation;
using Norse.AuthN.Services;

namespace Norse.AuthN.Components;

/// <summary>
///     Validator for <see cref="GetMaskedPersonalDataRequest" /> — the single source of truth for
///     masked-disclosure request validation on the whole platform, same dual-run shape as
///     <see cref="LoginRequestValidator" />: Blazilla runs it client-side against the wire type
///     directly, and Himinbjörg's generated
///     <c>
///         CommandRequestValidator&lt;TCommand,
///         GetMaskedPersonalDataRequest, MaskedPersonalDataResponse&gt;
///     </c>
///     reaches through the
///     server-sovereign command wrapper and runs this exact class again server-side.
///     <see cref="GetMyPersonalDataRequest" /> is an empty record by design — no validator exists for
///     it; nothing to validate <em>is</em> the point.
/// </summary>
public sealed class GetMaskedPersonalDataRequestValidator : AbstractValidator<GetMaskedPersonalDataRequest>
{
	/// <summary>Initializes a new instance of the <see cref="GetMaskedPersonalDataRequestValidator" /> class.</summary>
	public GetMaskedPersonalDataRequestValidator()
	{
		RuleFor(x => x.SubjectId)
			.NotEmpty();
	}
}
