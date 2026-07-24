using Norse.AuthN.Services;

namespace Norse.AuthN.Components.Tests;

public sealed class RegisterRequestValidatorTests
{
	readonly RegisterRequestValidator _validator = new();

	[Fact]
	void Rejects_malformed_email()
	{
		RegisterRequest request = new() { Email = "not-an-email", Password = "correct-horse-battery" };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeFalse();
	}

	[Fact]
	void Rejects_password_shorter_than_eight_characters()
	{
		RegisterRequest request = new() { Email = "user@example.com", Password = "short" };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeFalse();
	}

	[Fact]
	void Accepts_a_well_formed_request()
	{
		RegisterRequest request = new() { Email = "user@example.com", Password = "correct-horse-battery" };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeTrue();
	}
}
