using Norse.AuthN.Services;

namespace Norse.AuthN.Components.Tests;

public sealed class RemovePasskeyRequestValidatorTests
{
	readonly RemovePasskeyRequestValidator _validator = new();

	[Fact]
	void Rejects_empty_credential_id()
	{
		RemovePasskeyRequest request = new() { CredentialId = "" };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeFalse();
		result.Errors.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
			e => e.PropertyName.ShouldBe(nameof(RemovePasskeyRequest.CredentialId)),
			e => e.ErrorCode.ShouldBe("NotEmptyValidator"));
	}

	[Fact]
	void Accepts_a_well_formed_request()
	{
		RemovePasskeyRequest request = new() { CredentialId = "credential-id" };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeTrue();
	}
}
