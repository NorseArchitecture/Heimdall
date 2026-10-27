using Norse.AuthN.Services;

namespace Norse.AuthN.Components.Tests;

public sealed class ForgotPasswordRequestValidatorTests
{
	readonly ForgotPasswordRequestValidator _validator = new();

	[Fact]
	void Rejects_empty_email()
	{
		ForgotPasswordRequest request = new() { EmailInput = "" };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeFalse();
	}

	[Fact]
	void Rejects_malformed_email()
	{
		ForgotPasswordRequest request = new() { EmailInput = "not-an-email" };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeFalse();
	}

	[Fact]
	void Accepts_a_well_formed_request()
	{
		ForgotPasswordRequest request = new() { EmailInput = "user@example.com" };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeTrue();
	}
}
