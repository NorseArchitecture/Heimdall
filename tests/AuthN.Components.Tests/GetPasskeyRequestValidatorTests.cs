using Norse.AuthN.Services;

namespace Norse.AuthN.Components.Tests;

public sealed class GetPasskeyRequestValidatorTests
{
	readonly GetPasskeyRequestValidator _validator = new();

	[Fact]
	void Rejects_empty_credential_id()
	{
		GetPasskeyRequest request = new() { CredentialId = "" };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeFalse();
		result.Errors.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
			e => e.PropertyName.ShouldBe(nameof(GetPasskeyRequest.CredentialId)),
			e => e.ErrorCode.ShouldBe("NotEmptyValidator"));
	}

	[Fact]
	void Accepts_a_well_formed_request()
	{
		GetPasskeyRequest request = new() { CredentialId = "credential-id" };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeTrue();
	}
}
