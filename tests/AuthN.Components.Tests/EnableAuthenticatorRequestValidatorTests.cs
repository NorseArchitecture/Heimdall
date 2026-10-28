using Norse.AuthN.Services;

namespace Norse.AuthN.Components.Tests;

public sealed class EnableAuthenticatorRequestValidatorTests
{
	readonly EnableAuthenticatorRequestValidator _validator = new();

	[Fact]
	void Rejects_empty_code()
	{
		EnableAuthenticatorRequest request = new() { Code = "" };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeFalse();
		result.Errors.ShouldContain(e => e.PropertyName == nameof(EnableAuthenticatorRequest.Code) && e.ErrorCode == "NotEmptyValidator");
	}

	[Theory]
	[InlineData("123456")]
	[InlineData("123 456")]
	[InlineData("123-456")]
	void Accepts_six_digits_with_or_without_a_separator(string code) =>
		_validator.Validate(new EnableAuthenticatorRequest { Code = code }).IsValid.ShouldBeTrue();

	[Theory]
	[InlineData("12345")]
	[InlineData("12345678")]
	void Rejects_codes_outside_six_to_seven_characters(string code)
	{
		var result = _validator.Validate(new EnableAuthenticatorRequest { Code = code });

		result.IsValid.ShouldBeFalse();
		result.Errors.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
			e => e.PropertyName.ShouldBe(nameof(EnableAuthenticatorRequest.Code)),
			e => e.ErrorCode.ShouldBe("LengthValidator"));
	}
}
