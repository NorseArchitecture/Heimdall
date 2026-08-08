using Microsoft.Extensions.Logging.Abstractions;
using Norse.Abstractions.Contracts;
using Norse.AuthN.Services;
using Norse.Primitives;

namespace Norse.AuthN.Components.Tests;

public sealed class RegisterRequestValidatorTests
{
	// The email rule chain now ends in an async CustomAsync (Task 10) — sync FluentValidation
	// Validate() throws AsyncValidatorInvokedSynchronouslyException the moment a well-formed email
	// reaches it, so these pre-existing shape tests moved to ValidateAsync against a substitute
	// service wired to report "free" for every email, keeping their original intent untouched.
	static RegisterRequestValidator NewValidator()
	{
		var service = Substitute.For<IAuthenticationService>();
		service.EmailExists(Arg.Any<EmailExistsRequest>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult<Outcome<BoolResponse>>(new Success<BoolResponse>(new() { Value = false })));
		return new(service, NullLogger<RegisterRequestValidator>.Instance);
	}

	[Fact]
	async Task Rejects_malformed_email()
	{
		var validator = NewValidator();
		RegisterRequest request = new() { EmailInput = "not-an-email", Password = "correct-horse-battery" };

		var result = await validator.ValidateAsync(request, TestContext.Current.CancellationToken);

		result.IsValid.ShouldBeFalse();
	}

	[Fact]
	async Task Rejects_password_shorter_than_eight_characters()
	{
		var validator = NewValidator();
		RegisterRequest request = new() { EmailInput = "user@example.com", Password = "short" };

		var result = await validator.ValidateAsync(request, TestContext.Current.CancellationToken);

		result.IsValid.ShouldBeFalse();
	}

	[Fact]
	async Task Accepts_a_well_formed_request()
	{
		var validator = NewValidator();
		RegisterRequest request = new() { EmailInput = "user@example.com", Password = "correct-horse-battery" };

		var result = await validator.ValidateAsync(request, TestContext.Current.CancellationToken);

		result.IsValid.ShouldBeTrue();
	}
}
