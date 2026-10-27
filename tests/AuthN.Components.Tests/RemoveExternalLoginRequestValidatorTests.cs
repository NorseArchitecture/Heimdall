using Norse.AuthN.Services;

namespace Norse.AuthN.Components.Tests;

public sealed class RemoveExternalLoginRequestValidatorTests
{
	readonly RemoveExternalLoginRequestValidator _validator = new();

	[Fact]
	void Rejects_empty_login_provider()
	{
		RemoveExternalLoginRequest request = new() { LoginProvider = "", ProviderKey = "provider-key" };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeFalse();
		result.Errors.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
			e => e.PropertyName.ShouldBe(nameof(RemoveExternalLoginRequest.LoginProvider)),
			e => e.ErrorCode.ShouldBe("NotEmptyValidator"));
	}

	[Fact]
	void Rejects_empty_provider_key()
	{
		RemoveExternalLoginRequest request = new() { LoginProvider = "Google", ProviderKey = "" };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeFalse();
		result.Errors.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
			e => e.PropertyName.ShouldBe(nameof(RemoveExternalLoginRequest.ProviderKey)),
			e => e.ErrorCode.ShouldBe("NotEmptyValidator"));
	}

	[Fact]
	void Accepts_a_well_formed_request()
	{
		RemoveExternalLoginRequest request = new() { LoginProvider = "Google", ProviderKey = "provider-key" };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeTrue();
	}
}
