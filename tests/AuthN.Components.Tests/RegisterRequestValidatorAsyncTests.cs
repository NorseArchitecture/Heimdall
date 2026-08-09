using Microsoft.Extensions.Logging.Abstractions;
using Norse.Abstractions.Contracts;
using Norse.AuthN.Services;
using Norse.Primitives;

namespace Norse.AuthN.Components.Tests;

public sealed class RegisterRequestValidatorAsyncTests
{
	static RegisterRequestValidator NewValidator(Outcome<BoolResponse> emailExistsOutcome)
	{
		var service = Substitute.For<IAuthenticationService>();
		service.EmailExists(Arg.Any<EmailExistsRequest>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(emailExistsOutcome));
		return new(service, NullLogger<RegisterRequestValidator>.Instance);
	}

	static RegisterRequest ValidRequest() =>
		new() { EmailInput = "gyal@example.com", Password = "correct horse battery" };

	[Fact]
	async Task An_existing_email_fails_the_email_field()
	{
		var validator = NewValidator(new Success<BoolResponse>(new() { Value = true }));

		var result = await validator.ValidateAsync(ValidRequest(), TestContext.Current.CancellationToken);

		result.Errors.ShouldContain(e => e.PropertyName == nameof(RegisterRequest.Email));
	}

	[Fact]
	async Task A_free_email_passes()
	{
		var validator = NewValidator(new Success<BoolResponse>(new() { Value = false }));

		var result = await validator.ValidateAsync(ValidRequest(), TestContext.Current.CancellationToken);

		result.IsValid.ShouldBeTrue();
	}

	[Fact]
	async Task A_failed_lookup_blocks_with_a_could_not_verify_error()
	{
		var validator =
			NewValidator(new Failed(new() { Category = ErrorCategory.Fault, CorrelationId = Guid.NewGuid() }));

		var result = await validator.ValidateAsync(ValidRequest(), TestContext.Current.CancellationToken);

		result.IsValid.ShouldBeFalse();
	}

	[Fact]
	async Task A_malformed_email_short_circuits_before_the_service_is_called()
	{
		var service = Substitute.For<IAuthenticationService>();
		RegisterRequestValidator validator = new(service, NullLogger<RegisterRequestValidator>.Instance);
		RegisterRequest request = new() { EmailInput = "not-an-email", Password = "correct horse battery" };

		await validator.ValidateAsync(request, TestContext.Current.CancellationToken);

		await service.DidNotReceiveWithAnyArgs().EmailExists(default!, TestContext.Current.CancellationToken);
	}

	[Fact]
	async Task The_cancellation_token_propagates_into_the_service_call()
	{
		var service = Substitute.For<IAuthenticationService>();
		service.EmailExists(Arg.Any<EmailExistsRequest>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult<Outcome<BoolResponse>>(new Success<BoolResponse>(new() { Value = false })));
		RegisterRequestValidator validator = new(service, NullLogger<RegisterRequestValidator>.Instance);
		using CancellationTokenSource source = new();

		await validator.ValidateAsync(ValidRequest(), source.Token);

		await service.Received(1).EmailExists(Arg.Any<EmailExistsRequest>(), source.Token);
	}
}
