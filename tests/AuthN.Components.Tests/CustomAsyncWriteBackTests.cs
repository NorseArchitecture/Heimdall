using FluentValidation;

namespace Norse.AuthN.Components.Tests;

/// <summary>
/// A test-local proof (never shipped) that FluentValidation's <c>CustomAsync</c> write-back onto
/// <c>context.InstanceToValidate</c> composes correctly with sync rules — the general pattern
/// <see cref="RegisterRequestValidator"/>'s email-exists rule rides, proven in isolation from the
/// register flow itself.
/// </summary>
public sealed class CustomAsyncWriteBackTests
{
	sealed class Address
	{
		public string PostalCode { get; set; } = "";
		public string City { get; set; } = "";
	}

	sealed class AddressValidator : AbstractValidator<Address>
	{
		public AddressValidator(Func<string, CancellationToken, Task<string?>> lookupCity)
		{
			RuleFor(a => a.PostalCode).NotEmpty();
			RuleFor(a => a.PostalCode).CustomAsync(async (zip, context, cancellationToken) =>
			{
				var city = await lookupCity(zip, cancellationToken);
				if (city is null)
				{
					context.AddFailure("Zip code not found.");
					return;
				}

				context.InstanceToValidate.City = city;
			});
		}
	}

	[Fact]
	async Task A_successful_lookup_writes_back_onto_the_model_and_composes_with_sync_rules()
	{
		AddressValidator validator = new((_, _) => Task.FromResult<string?>("Thibodaux"));
		Address address = new() { PostalCode = "70301" };

		var result = await validator.ValidateAsync(address, TestContext.Current.CancellationToken);

		result.IsValid.ShouldBeTrue();
		address.City.ShouldBe("Thibodaux");
	}

	[Fact]
	async Task A_missing_lookup_fails_the_field_without_writing_back()
	{
		AddressValidator validator = new((_, _) => Task.FromResult<string?>(null));
		Address address = new() { PostalCode = "00000" };

		var result = await validator.ValidateAsync(address, TestContext.Current.CancellationToken);

		result.IsValid.ShouldBeFalse();
		address.City.ShouldBeEmpty();
	}
}
