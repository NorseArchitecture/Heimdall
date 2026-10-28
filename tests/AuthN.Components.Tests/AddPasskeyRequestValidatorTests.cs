using Norse.AuthN.Services;

namespace Norse.AuthN.Components.Tests;

public sealed class AddPasskeyRequestValidatorTests
{
	readonly AddPasskeyRequestValidator _validator = new();

	[Fact]
	void Rejects_empty_credential_json()
	{
		AddPasskeyRequest request = new() { CredentialJson = "", AttestationState = "state" };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeFalse();
		result.Errors.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
			e => e.PropertyName.ShouldBe(nameof(AddPasskeyRequest.CredentialJson)),
			e => e.ErrorCode.ShouldBe("NotEmptyValidator"));
	}

	[Fact]
	void Rejects_empty_attestation_state()
	{
		AddPasskeyRequest request = new() { CredentialJson = "{}", AttestationState = "" };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeFalse();
		result.Errors.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
			e => e.PropertyName.ShouldBe(nameof(AddPasskeyRequest.AttestationState)),
			e => e.ErrorCode.ShouldBe("NotEmptyValidator"));
	}

	[Fact]
	void Accepts_a_well_formed_request()
	{
		AddPasskeyRequest request = new() { CredentialJson = "{}", AttestationState = "state" };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeTrue();
	}
}
