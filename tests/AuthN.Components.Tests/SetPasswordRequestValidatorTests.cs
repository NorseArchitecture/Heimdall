using Norse.AuthN.Services;

namespace Norse.AuthN.Components.Tests;

public sealed class SetPasswordRequestValidatorTests
{
	readonly SetPasswordRequestValidator _validator = new();

	[Fact]
	void Rejects_empty_new_password()
	{
		SetPasswordRequest request = new() { NewPassword = "" };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeFalse();
		result.Errors.ShouldContain(e => e.PropertyName == nameof(SetPasswordRequest.NewPassword) && e.ErrorCode == "NotEmptyValidator");
	}

	[Fact]
	void Rejects_a_new_password_shorter_than_eight_characters()
	{
		SetPasswordRequest request = new() { NewPassword = "seven77" };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeFalse();
		result.Errors.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
			e => e.PropertyName.ShouldBe(nameof(SetPasswordRequest.NewPassword)),
			e => e.ErrorCode.ShouldBe("MinimumLengthValidator"));
	}

	[Fact]
	void Accepts_a_well_formed_request()
	{
		SetPasswordRequest request = new() { NewPassword = "correct-horse" };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeTrue();
	}
}
