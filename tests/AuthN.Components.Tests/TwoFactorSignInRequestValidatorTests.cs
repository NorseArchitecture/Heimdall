using Norse.AuthN.Services;

namespace Norse.AuthN.Components.Tests;

public sealed class TwoFactorSignInRequestValidatorTests
{
	readonly TwoFactorSignInRequestValidator _validator = new();

	[Fact]
	void Rejects_empty_challenge()
	{
		TwoFactorSignInRequest request = new() { Challenge = "", Code = "123456" };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeFalse();
	}

	[Fact]
	void Rejects_empty_code()
	{
		TwoFactorSignInRequest request = new() { Challenge = "c", Code = "" };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeFalse();
	}

	[Fact]
	void Accepts_a_well_formed_request()
	{
		TwoFactorSignInRequest request = new() { Challenge = "c", Code = "123456", RememberMachine = true };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeTrue();
	}

	[Theory]
	[InlineData("123456")]
	[InlineData("123 456")]
	[InlineData("123-456")]
	void Accepts_six_digits_with_or_without_a_separator(string code) =>
		_validator.Validate(new TwoFactorSignInRequest { Challenge = "c", Code = code }).IsValid.ShouldBeTrue();

	[Theory]
	[InlineData("12345")]
	[InlineData("12345678")]
	void Rejects_codes_outside_six_to_seven_characters(string code) =>
		_validator.Validate(new TwoFactorSignInRequest { Challenge = "c", Code = code }).IsValid.ShouldBeFalse();
}
