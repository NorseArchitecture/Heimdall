using Norse.AuthN.Services;

namespace Norse.AuthN.Components.Tests;

public sealed class ConfirmEmailChangeRequestValidatorTests
{
	readonly ConfirmEmailChangeRequestValidator _validator = new();

	[Fact]
	void Rejects_empty_user_id()
	{
		ConfirmEmailChangeRequest request = new() { UserId = "", EmailInput = "user@example.com", Code = "change-code" };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeFalse();
	}

	[Fact]
	void Rejects_empty_email()
	{
		ConfirmEmailChangeRequest request = new() { UserId = "user-1", EmailInput = "", Code = "change-code" };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeFalse();
	}

	[Fact]
	void Rejects_malformed_email()
	{
		ConfirmEmailChangeRequest request = new() { UserId = "user-1", EmailInput = "not-an-email", Code = "change-code" };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeFalse();
	}

	[Fact]
	void Rejects_empty_code()
	{
		ConfirmEmailChangeRequest request = new() { UserId = "user-1", EmailInput = "user@example.com", Code = "" };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeFalse();
	}

	[Fact]
	void Accepts_a_well_formed_request()
	{
		ConfirmEmailChangeRequest request = new() { UserId = "user-1", EmailInput = "user@example.com", Code = "change-code" };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeTrue();
	}
}
