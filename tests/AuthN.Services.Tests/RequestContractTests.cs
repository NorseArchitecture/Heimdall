using Microsoft.AspNetCore.Authorization;
using System.Reflection;
using System.Runtime.Serialization;
using System.ServiceModel;
using Norse.Abstractions.Contracts;
using Norse.Primitives;
using Norse.Primitives.Pii;

namespace Norse.AuthN.Services.Tests;

/// <summary>
///     Purity test — locks the ratified wire-purity ruling against future drift. Wire
///     <c>[DataContract]</c> records carry no mediator marker and no <c>[Authorize]</c> at all; those
///     belong solely to Himinbjörg's server-sovereign command wrappers.
/// </summary>
public sealed class RequestContractTests
{
	static Type[] NewRecords =>
	[
		typeof(TwoFactorSignInRequest), typeof(RecoveryCodeSignInRequest), typeof(ForgotPasswordRequest),
		typeof(ResetPasswordRequest), typeof(ConfirmEmailRequest), typeof(ConfirmEmailChangeRequest),
		typeof(ResendEmailConfirmationRequest), typeof(AccountStatusResponse), typeof(SetPhoneNumberRequest),
		typeof(ChangePasswordRequest), typeof(SetPasswordRequest), typeof(ChangeEmailRequest),
		typeof(ExternalLoginsResponse), typeof(ExternalLoginEntry), typeof(ExternalProviderEntry),
		typeof(RemoveExternalLoginRequest), typeof(DeletePersonalDataRequest)
	];

	static MethodInfo Operation(Type contract, Type request) =>
		contract.GetMethods()
			.Single(m => m.GetParameters() is [var first, ..] && first.ParameterType == request);

	[Fact]
	void AuthN_Services_does_not_reference_the_mediator_law_assembly()
	{
		// The mediator marker family (IRequest<T>/ICommandRequest<T>/IQueryRequest<T>) moved into
		// Norse.Abstractions.Web.Server (server-only law, 2026-07-27 amendment) precisely so a wire
		// assembly could no longer even name them, let alone implement them. Asserting a wire type
		// "doesn't implement IRequest<>" is no longer expressible from here — this assembly cannot
		// reference the type at all — so the stronger, structural check is that the assembly
		// reference itself is absent. That absence is the actual enforcement mechanism.
		typeof(IAuthenticationService).Assembly.GetReferencedAssemblies()
			.Any(a => a.Name == "Norse.Abstractions.Web.Server")
			.ShouldBeFalse(
				"Norse.AuthN.Services must not reference Norse.Abstractions.Web.Server — mediator law is structurally invisible to the wire assembly, not merely unused by convention.");
	}

	[Fact]
	void Wire_records_carry_no_Authorize_attribute()
	{
		foreach (var wireType in (Type[])
			[
				typeof(LoginRequest), typeof(RegisterRequest), typeof(GetMyPersonalDataRequest),
				typeof(GetMaskedPersonalDataRequest), typeof(PersonalDataResponse), typeof(MaskedPersonalDataResponse),
				typeof(EmailExistsRequest), typeof(TwoFactorSignInRequest), typeof(RecoveryCodeSignInRequest),
				typeof(ForgotPasswordRequest), typeof(ResetPasswordRequest), typeof(ConfirmEmailRequest),
				typeof(ConfirmEmailChangeRequest), typeof(ResendEmailConfirmationRequest), typeof(AccountStatusResponse),
				typeof(SetPhoneNumberRequest), typeof(ChangePasswordRequest), typeof(SetPasswordRequest),
				typeof(ChangeEmailRequest), typeof(ExternalLoginsResponse), typeof(ExternalLoginEntry),
				typeof(ExternalProviderEntry), typeof(RemoveExternalLoginRequest), typeof(DeletePersonalDataRequest)
			])
			wireType.GetCustomAttribute<AuthorizeAttribute>()
				.ShouldBeNull(
					$"{wireType.Name} must not carry [Authorize] — that policy lives on Himinbjörg's command wrapper.");
	}

	[Fact]
	void Every_service_method_ends_with_a_trailing_cancellation_token()
	{
		foreach (var method in typeof(IAuthenticationService).GetMethods()
			.Concat(typeof(IIdentityService).GetMethods())
			.Concat(typeof(IAccountService).GetMethods()))
			method.GetParameters()[^1].ParameterType.ShouldBe(typeof(CancellationToken));
	}

	[Fact]
	void Email_exists_is_declared_on_the_authentication_contract()
	{
		var method = typeof(IAuthenticationService).GetMethod("EmailExists");

		method.ShouldNotBeNull();
		method.ReturnType.ShouldBe(typeof(Task<Outcome<BoolResponse>>));
	}

	[Fact]
	void Every_service_operation_carries_the_operation_contract_attribute()
	{
		// A code-first gRPC method without [OperationContract] is a silently dead endpoint —
		// this lock covers every current and future operation on every contract.
		Type[] contracts = [typeof(IAuthenticationService), typeof(IIdentityService), typeof(IAccountService)];

		foreach (var method in contracts.SelectMany(c => c.GetMethods()))
			method.GetCustomAttribute<OperationContractAttribute>()
				.ShouldNotBeNull($"{method.DeclaringType!.Name}.{method.Name} is missing [OperationContract]");
	}

	[Fact]
	void The_seven_anonymous_lifecycle_operations_are_declared_with_the_ruled_return_types()
	{
		Type[] navigation = [typeof(TwoFactorSignInRequest), typeof(RecoveryCodeSignInRequest), typeof(ForgotPasswordRequest), typeof(ResetPasswordRequest), typeof(ConfirmEmailChangeRequest)];
		Type[] acknowledgment = [typeof(ConfirmEmailRequest), typeof(ResendEmailConfirmationRequest)];
		foreach (var request in navigation)
			Operation(typeof(IAuthenticationService), request).ReturnType.ShouldBe(typeof(Task<Outcome<NavigationResult>>));
		foreach (var request in acknowledgment)
			Operation(typeof(IAuthenticationService), request).ReturnType.ShouldBe(typeof(Task<Outcome<BoolResponse>>));
	}

	[Fact]
	void Every_wire_record_is_a_data_contract_with_ordered_members()
	{
		foreach (var wireType in NewRecords)
		{
			wireType.GetCustomAttribute<DataContractAttribute>().ShouldNotBeNull(wireType.Name);
			wireType.GetProperties().Where(p => !p.Name.EndsWith("Input", StringComparison.Ordinal))
				.Select(p => p.GetCustomAttribute<DataMemberAttribute>()?.Order)
				.ShouldAllBe(order => order.HasValue, wireType.Name);
		}
	}

	[Fact]
	void Email_input_buffers_are_never_serialized()
	{
		foreach (var wireType in new[] { typeof(ForgotPasswordRequest), typeof(ResetPasswordRequest), typeof(ConfirmEmailChangeRequest), typeof(ResendEmailConfirmationRequest) })
			wireType.GetProperty("EmailInput")!.GetCustomAttribute<DataMemberAttribute>().ShouldBeNull(wireType.Name);
	}

	[Fact]
	void The_account_contract_is_declared_with_the_ruled_name_and_return_types()
	{
		typeof(IAccountService).GetCustomAttribute<ServiceContractAttribute>()!.Name.ShouldBe("grpc.account.v1.AccountService");
		Type[] navigation = [typeof(SetPhoneNumberRequest), typeof(ChangePasswordRequest), typeof(SetPasswordRequest), typeof(RemoveExternalLoginRequest), typeof(DeletePersonalDataRequest)];
		foreach (var request in navigation)
			Operation(typeof(IAccountService), request).ReturnType.ShouldBe(typeof(Task<Outcome<NavigationResult>>));
		Operation(typeof(IAccountService), typeof(ChangeEmailRequest)).ReturnType.ShouldBe(typeof(Task<Outcome<BoolResponse>>));
		typeof(IAccountService).GetMethod(nameof(IAccountService.GetAccountStatus))!.ReturnType.ShouldBe(typeof(Task<Outcome<AccountStatusResponse>>));
		typeof(IAccountService).GetMethod(nameof(IAccountService.GetExternalLogins))!.ReturnType.ShouldBe(typeof(Task<Outcome<ExternalLoginsResponse>>));
	}

	[Fact]
	void The_request_less_account_operations_take_only_a_cancellation_token()
	{
		foreach (var name in new[] { nameof(IAccountService.GetAccountStatus), nameof(IAccountService.GetExternalLogins) })
			typeof(IAccountService).GetMethod(name)!.GetParameters().ShouldHaveSingleItem().ParameterType.ShouldBe(typeof(CancellationToken));
	}

	[Fact]
	void Phone_and_new_email_buffers_are_never_serialized()
	{
		typeof(SetPhoneNumberRequest).GetProperty(nameof(SetPhoneNumberRequest.PhoneInput))!.GetCustomAttribute<DataMemberAttribute>().ShouldBeNull();
		typeof(ChangeEmailRequest).GetProperty(nameof(ChangeEmailRequest.NewEmailInput))!.GetCustomAttribute<DataMemberAttribute>().ShouldBeNull();
	}

	[Fact]
	void The_phone_buffer_restamps_on_every_assignment()
	{
		SetPhoneNumberRequest request = new() { PhoneInput = "+1 (555) 123-4567" };
		request.Phone.ShouldNotBeNull().TryGetValue(out Success<PhoneNumber> parsed).ShouldBeTrue();
		parsed.Value.WireValue.ShouldBe("+15551234567");
		request.PhoneInput = "";
		request.Phone.ShouldBeNull();
		request.PhoneInput = "   ";
		request.Phone.ShouldBeNull();
	}

	[Fact]
	void Response_collections_default_to_empty_not_null()
	{
		ExternalLoginsResponse response = new() { CanRemove = false };
		response.Logins.ShouldBeEmpty();
		response.AvailableProviders.ShouldBeEmpty();
	}
}
