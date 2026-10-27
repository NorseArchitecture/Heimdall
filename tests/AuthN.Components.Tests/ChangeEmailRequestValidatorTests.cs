using Norse.AuthN.Services;

namespace Norse.AuthN.Components.Tests;

public sealed class ChangeEmailRequestValidatorTests
{
	readonly ChangeEmailRequestValidator _validator = new();

	[Fact]
	void Rejects_empty_new_email()
	{
		ChangeEmailRequest request = new() { NewEmailInput = "" };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeFalse();
		result.Errors.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
			e => e.PropertyName.ShouldBe(nameof(ChangeEmailRequest.NewEmail)),
			e => e.ErrorMessage.ShouldBe("Enter your new email address."));
	}

	[Fact]
	void Rejects_malformed_new_email()
	{
		ChangeEmailRequest request = new() { NewEmailInput = "not-an-email" };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeFalse();
		result.Errors.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
			e => e.PropertyName.ShouldBe(nameof(ChangeEmailRequest.NewEmail)),
			e => e.ErrorMessage.ShouldBe("Enter a valid email address (local@domain.tld)."));
	}

	[Fact]
	void Accepts_a_well_formed_request()
	{
		ChangeEmailRequest request = new() { NewEmailInput = "user@example.com" };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeTrue();
	}
}
