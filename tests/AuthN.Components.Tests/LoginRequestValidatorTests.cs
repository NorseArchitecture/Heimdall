using Norse.AuthN.Services;

namespace Norse.AuthN.Components.Tests;

public sealed class LoginRequestValidatorTests
{
	readonly LoginRequestValidator _validator = new();

	[Fact]
	void Rejects_empty_email()
	{
		LoginRequest request = new() { Email = "", Password = "correct-horse" };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeFalse();
	}

	[Fact]
	void Rejects_malformed_email()
	{
		LoginRequest request = new() { Email = "not-an-email", Password = "correct-horse" };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeFalse();
	}

	[Fact]
	void Rejects_empty_password()
	{
		LoginRequest request = new() { Email = "user@example.com", Password = "" };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeFalse();
	}

	[Fact]
	void Accepts_a_well_formed_request()
	{
		LoginRequest request = new() { Email = "user@example.com", Password = "correct-horse" };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeTrue();
	}
}
