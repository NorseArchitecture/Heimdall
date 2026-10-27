using Norse.AuthN.Services;
using Norse.Primitives;
using Norse.Primitives.Pii;

namespace Norse.AuthN.Components.Tests;

public sealed class SetPhoneNumberRequestValidatorTests
{
	const string FormatMessage = "Enter a valid phone number (+15551234567).";

	readonly SetPhoneNumberRequestValidator _validator = new();

	[Fact]
	void An_empty_phone_is_accepted()
	{
		SetPhoneNumberRequest request = new() { PhoneInput = "" };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeTrue();
	}

	[Fact]
	void A_phone_without_the_plus_prefix_is_rejected()
	{
		SetPhoneNumberRequest request = new() { PhoneInput = "5551234567" };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeFalse();
		result.Errors.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
			e => e.PropertyName.ShouldBe(nameof(SetPhoneNumberRequest.Phone)),
			e => e.ErrorMessage.ShouldBe(FormatMessage));
	}

	[Fact]
	void An_empty_failure_set_directly_on_the_stamp_is_rejected()
	{
		SetPhoneNumberRequest request = new() { PhoneInput = "", Phone = PhoneNumber.Parse(string.Empty) };
		request.Phone.ShouldNotBeNull().TryGetValue(out Failure failure).ShouldBeTrue();
		failure.Reason.ShouldBe(ParseFailure.Empty);

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeFalse();
		result.Errors.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
			e => e.PropertyName.ShouldBe(nameof(SetPhoneNumberRequest.Phone)),
			e => e.ErrorMessage.ShouldBe(FormatMessage));
	}

	[Fact]
	void Accepts_a_well_formed_request()
	{
		SetPhoneNumberRequest request = new() { PhoneInput = "+15551234567" };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeTrue();
	}
}
