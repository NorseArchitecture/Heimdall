using Norse.AuthN.Services;

namespace Norse.AuthN.Components.Tests;

public sealed class DeletePersonalDataRequestValidatorTests
{
	readonly DeletePersonalDataRequestValidator _validator = new();

	[Fact]
	void An_empty_password_is_accepted()
	{
		DeletePersonalDataRequest request = new() { Password = "" };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeTrue();
	}

	[Fact]
	void A_257_character_password_is_rejected()
	{
		DeletePersonalDataRequest request = new() { Password = new('x', 257) };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeFalse();
		result.Errors.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
			e => e.PropertyName.ShouldBe(nameof(DeletePersonalDataRequest.Password)),
			e => e.ErrorCode.ShouldBe("MaximumLengthValidator"));
	}

	[Fact]
	void Accepts_a_well_formed_request()
	{
		DeletePersonalDataRequest request = new() { Password = "correct-horse" };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeTrue();
	}
}
