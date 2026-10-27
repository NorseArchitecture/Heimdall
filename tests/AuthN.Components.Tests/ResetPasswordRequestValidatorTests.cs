using Norse.AuthN.Services;

namespace Norse.AuthN.Components.Tests;

public sealed class ResetPasswordRequestValidatorTests
{
	readonly ResetPasswordRequestValidator _validator = new();

	[Fact]
	void Rejects_empty_email()
	{
		ResetPasswordRequest request = new() { EmailInput = "", Code = "reset-code", Password = "correct-horse" };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeFalse();
	}

	[Fact]
	void Rejects_malformed_email()
	{
		ResetPasswordRequest request = new() { EmailInput = "not-an-email", Code = "reset-code", Password = "correct-horse" };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeFalse();
	}

	[Fact]
	void Rejects_empty_code()
	{
		ResetPasswordRequest request = new() { EmailInput = "user@example.com", Code = "", Password = "correct-horse" };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeFalse();
	}

	[Fact]
	void Rejects_empty_password()
	{
		ResetPasswordRequest request = new() { EmailInput = "user@example.com", Code = "reset-code", Password = "" };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeFalse();
	}

	[Fact]
	void Rejects_a_password_shorter_than_eight_characters()
	{
		ResetPasswordRequest request = new() { EmailInput = "user@example.com", Code = "reset-code", Password = "seven77" };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeFalse();
	}

	[Fact]
	void Accepts_a_well_formed_request()
	{
		ResetPasswordRequest request = new() { EmailInput = "user@example.com", Code = "reset-code", Password = "correct-horse" };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeTrue();
	}
}
