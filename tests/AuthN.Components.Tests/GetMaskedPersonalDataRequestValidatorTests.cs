using Norse.AuthN.Services;

namespace Norse.AuthN.Components.Tests;

public sealed class GetMaskedPersonalDataRequestValidatorTests
{
	readonly GetMaskedPersonalDataRequestValidator _validator = new();

	[Fact]
	void Rejects_empty_subject_id()
	{
		GetMaskedPersonalDataRequest request = new() { SubjectId = Guid.Empty };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeFalse();
	}

	[Fact]
	void Accepts_a_well_formed_request()
	{
		GetMaskedPersonalDataRequest request = new() { SubjectId = Guid.NewGuid() };

		var result = _validator.Validate(request);

		result.IsValid.ShouldBeTrue();
	}
}
