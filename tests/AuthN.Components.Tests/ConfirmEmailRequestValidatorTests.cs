using Norse.AuthN.Services;

namespace Norse.AuthN.Components.Tests;

public sealed class ConfirmEmailRequestValidatorTests
{
	readonly ConfirmEmailRequestValidator _validator = new();

	[Fact]
	void Rejects_empty_user_id()
	{
		ConfirmEmailRequest request = new() { UserId = "", Code = "confirm-code" };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeFalse();
	}

	[Fact]
	void Rejects_empty_code()
	{
		ConfirmEmailRequest request = new() { UserId = "user-1", Code = "" };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeFalse();
	}

	[Fact]
	void Accepts_a_well_formed_request()
	{
		ConfirmEmailRequest request = new() { UserId = "user-1", Code = "confirm-code" };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeTrue();
	}
}
