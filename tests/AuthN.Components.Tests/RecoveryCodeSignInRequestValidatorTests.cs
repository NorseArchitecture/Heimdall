using Norse.AuthN.Services;

namespace Norse.AuthN.Components.Tests;

public sealed class RecoveryCodeSignInRequestValidatorTests
{
	readonly RecoveryCodeSignInRequestValidator _validator = new();

	[Fact]
	void Rejects_empty_challenge()
	{
		RecoveryCodeSignInRequest request = new() { Challenge = "", RecoveryCode = "abcde-12345" };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeFalse();
	}

	[Fact]
	void Rejects_empty_recovery_code()
	{
		RecoveryCodeSignInRequest request = new() { Challenge = "c", RecoveryCode = "" };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeFalse();
	}

	[Fact]
	void Accepts_a_well_formed_request()
	{
		RecoveryCodeSignInRequest request = new() { Challenge = "c", RecoveryCode = "abcde-12345" };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeTrue();
	}
}
