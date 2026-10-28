using Norse.AuthN.Services;

namespace Norse.AuthN.Components.Tests;

public sealed class RevealRecoveryCodesRequestValidatorTests
{
	readonly RevealRecoveryCodesRequestValidator _validator = new();

	[Fact]
	void Rejects_empty_handoff()
	{
		RevealRecoveryCodesRequest request = new() { Handoff = "" };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeFalse();
		result.Errors.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
			e => e.PropertyName.ShouldBe(nameof(RevealRecoveryCodesRequest.Handoff)),
			e => e.ErrorCode.ShouldBe("NotEmptyValidator"));
	}

	[Fact]
	void Accepts_a_well_formed_request()
	{
		RevealRecoveryCodesRequest request = new() { Handoff = "handoff" };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeTrue();
	}
}
