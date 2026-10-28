using Norse.AuthN.Services;

namespace Norse.AuthN.Components.Tests;

public sealed class PasskeySignInRequestValidatorTests
{
	readonly PasskeySignInRequestValidator _validator = new();

	[Fact]
	void Rejects_empty_credential_json()
	{
		PasskeySignInRequest request = new() { CredentialJson = "", AssertionState = "state" };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeFalse();
		result.Errors.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
			e => e.PropertyName.ShouldBe(nameof(PasskeySignInRequest.CredentialJson)),
			e => e.ErrorCode.ShouldBe("NotEmptyValidator"));
	}

	[Fact]
	void Rejects_empty_assertion_state()
	{
		PasskeySignInRequest request = new() { CredentialJson = "{}", AssertionState = "" };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeFalse();
		result.Errors.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
			e => e.PropertyName.ShouldBe(nameof(PasskeySignInRequest.AssertionState)),
			e => e.ErrorCode.ShouldBe("NotEmptyValidator"));
	}

	[Fact]
	void Accepts_a_well_formed_request_without_a_return_url()
	{
		PasskeySignInRequest request = new() { CredentialJson = "{}", AssertionState = "state", ReturnUrl = "" };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeTrue();
	}

	[Fact]
	void Accepts_a_well_formed_request_with_a_return_url()
	{
		PasskeySignInRequest request = new() { CredentialJson = "{}", AssertionState = "state", RememberMe = true, ReturnUrl = "/Account/Manage" };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeTrue();
	}
}
