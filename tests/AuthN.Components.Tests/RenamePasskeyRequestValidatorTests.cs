using Norse.AuthN.Services;

namespace Norse.AuthN.Components.Tests;

public sealed class RenamePasskeyRequestValidatorTests
{
	readonly RenamePasskeyRequestValidator _validator = new();

	[Fact]
	void Rejects_empty_credential_id()
	{
		RenamePasskeyRequest request = new() { CredentialId = "", Name = "Laptop" };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeFalse();
		result.Errors.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
			e => e.PropertyName.ShouldBe(nameof(RenamePasskeyRequest.CredentialId)),
			e => e.ErrorCode.ShouldBe("NotEmptyValidator"));
	}

	[Fact]
	void Rejects_empty_name()
	{
		RenamePasskeyRequest request = new() { CredentialId = "credential-id", Name = "" };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeFalse();
		result.Errors.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
			e => e.PropertyName.ShouldBe(nameof(RenamePasskeyRequest.Name)),
			e => e.ErrorCode.ShouldBe("NotEmptyValidator"));
	}

	[Fact]
	void Rejects_a_name_longer_than_two_hundred_characters()
	{
		RenamePasskeyRequest request = new() { CredentialId = "credential-id", Name = new('a', 201) };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeFalse();
		result.Errors.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
			e => e.PropertyName.ShouldBe(nameof(RenamePasskeyRequest.Name)),
			e => e.ErrorCode.ShouldBe("MaximumLengthValidator"));
	}

	[Fact]
	void Accepts_a_name_of_exactly_two_hundred_characters()
	{
		RenamePasskeyRequest request = new() { CredentialId = "credential-id", Name = new('a', 200) };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeTrue();
	}
}
