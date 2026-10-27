using Norse.AuthN.Services;

namespace Norse.AuthN.Components.Tests;

public sealed class ChangePasswordRequestValidatorTests
{
	readonly ChangePasswordRequestValidator _validator = new();

	[Fact]
	void Rejects_empty_old_password()
	{
		ChangePasswordRequest request = new() { OldPassword = "", NewPassword = "correct-horse" };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeFalse();
		result.Errors.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
			e => e.PropertyName.ShouldBe(nameof(ChangePasswordRequest.OldPassword)),
			e => e.ErrorCode.ShouldBe("NotEmptyValidator"));
	}

	[Fact]
	void Rejects_empty_new_password()
	{
		ChangePasswordRequest request = new() { OldPassword = "battery-staple", NewPassword = "" };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeFalse();
		result.Errors.ShouldContain(e => e.PropertyName == nameof(ChangePasswordRequest.NewPassword) && e.ErrorCode == "NotEmptyValidator");
	}

	[Fact]
	void An_all_empty_form_does_not_claim_the_new_password_must_differ()
	{
		ChangePasswordRequest request = new() { OldPassword = "", NewPassword = "" };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeFalse();
		result.Errors.ShouldNotContain(e => e.ErrorMessage == "Your new password must differ from your current password.");
	}

	[Fact]
	void Rejects_a_new_password_shorter_than_eight_characters()
	{
		ChangePasswordRequest request = new() { OldPassword = "battery-staple", NewPassword = "seven77" };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeFalse();
		result.Errors.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
			e => e.PropertyName.ShouldBe(nameof(ChangePasswordRequest.NewPassword)),
			e => e.ErrorCode.ShouldBe("MinimumLengthValidator"));
	}

	[Fact]
	void A_new_password_equal_to_the_old_one_is_rejected()
	{
		ChangePasswordRequest request = new() { OldPassword = "correct-horse", NewPassword = "correct-horse" };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeFalse();
		result.Errors.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
			e => e.PropertyName.ShouldBe(nameof(ChangePasswordRequest.NewPassword)),
			e => e.ErrorMessage.ShouldBe("Your new password must differ from your current password."));
	}

	[Fact]
	void Accepts_a_well_formed_request()
	{
		ChangePasswordRequest request = new() { OldPassword = "battery-staple", NewPassword = "correct-horse" };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeTrue();
	}
}
