namespace Norse.AuthN.Components.Tests;

public sealed class LoginRequestValidatorTests
{
	readonly LoginRequestValidator _validator = new();

	[Fact]
	void Rejects_empty_email()
	{
		var request = new LoginRequest { Email = "", Password = "correct-horse" };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeFalse();
	}

	[Fact]
	void Rejects_malformed_email()
	{
		var request = new LoginRequest { Email = "not-an-email", Password = "correct-horse" };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeFalse();
	}

	[Fact]
	void Rejects_empty_password()
	{
		var request = new LoginRequest { Email = "user@example.com", Password = "" };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeFalse();
	}

	[Fact]
	void Accepts_a_well_formed_request()
	{
		var request = new LoginRequest { Email = "user@example.com", Password = "correct-horse" };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeTrue();
	}
}
