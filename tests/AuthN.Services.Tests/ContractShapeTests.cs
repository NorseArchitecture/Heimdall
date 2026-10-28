using System.Reflection;
using System.Runtime.Serialization;
using System.ServiceModel;
using Norse.Abstractions.Contracts;
using ProtoBuf;

namespace Norse.AuthN.Services.Tests;

/// <summary>
///     Shape locks for the Train 3 wire tier — the three new contracts, their records, and
///     <see cref="LoginRequest.ReturnUrl" />. Every later Heimdall, Himinbjörg and Bragi task builds on
///     these names, so each name, return type and member order is pinned here.
/// </summary>
public sealed class ContractShapeTests
{
	static readonly (Type Contract, string ServiceName, (string Name, Type? Request, Type Response)[] Operations)[] _contracts =
	[
		(typeof(ITwoFactorService), "grpc.twofactor.v1.TwoFactorService",
		[
			("GetAuthenticatorSetup", null, typeof(AuthenticatorSetupResponse)),
			("EnableAuthenticator", typeof(EnableAuthenticatorRequest), typeof(NavigationResult)),
			("RevealRecoveryCodes", typeof(RevealRecoveryCodesRequest), typeof(RecoveryCodesResponse)),
			("ResetAuthenticator", null, typeof(NavigationResult)),
			("DisableTwoFactor", null, typeof(NavigationResult)),
			("GenerateRecoveryCodes", null, typeof(RecoveryCodesResponse)),
			("ForgetTwoFactorClient", null, typeof(NavigationResult))
		]),
		(typeof(IPasskeyService), "grpc.passkey.v1.PasskeyService",
		[
			("GetPasskeys", null, typeof(PasskeysResponse)),
			("GetPasskey", typeof(GetPasskeyRequest), typeof(PasskeyEntry)),
			("GetPasskeyCreationOptions", null, typeof(PasskeyCreationOptionsResponse)),
			("AddPasskey", typeof(AddPasskeyRequest), typeof(NavigationResult)),
			("RenamePasskey", typeof(RenamePasskeyRequest), typeof(NavigationResult)),
			("RemovePasskey", typeof(RemovePasskeyRequest), typeof(BoolResponse))
		]),
		(typeof(ISignInMethodsService), "grpc.signinmethods.v1.SignInMethodsService",
		[
			("GetSignInMethods", typeof(GetSignInMethodsRequest), typeof(SignInMethodsResponse)),
			("GetPasskeyRequestOptions", null, typeof(PasskeyRequestOptionsResponse)),
			("PasskeySignIn", typeof(PasskeySignInRequest), typeof(NavigationResult))
		])
	];

	[Fact]
	void Each_train_3_contract_has_its_service_name_and_operations()
	{
		foreach (var (contract, serviceName, operations) in _contracts)
		{
			contract.GetCustomAttribute<ServiceContractAttribute>().ShouldNotBeNull(contract.Name).Name.ShouldBe(serviceName);
			contract.GetMethods().Select(m => m.Name).ShouldBe(operations.Select(o => o.Name), ignoreOrder: true);

			foreach (var (name, request, response) in operations)
			{
				var method = contract.GetMethod(name).ShouldNotBeNull($"{contract.Name}.{name}");
				method.GetCustomAttribute<OperationContractAttribute>().ShouldNotBeNull($"{contract.Name}.{name}");
				method.ReturnType.ShouldBe(typeof(Task<>).MakeGenericType(typeof(Outcome<>).MakeGenericType(response)), $"{contract.Name}.{name}");

				var parameters = method.GetParameters();
				parameters.Select(p => p.ParameterType)
					.ShouldBe(request is null ? [typeof(CancellationToken)] : [request, typeof(CancellationToken)], $"{contract.Name}.{name}");
				parameters[^1].Name.ShouldBe("cancellationToken");
				parameters[^1].HasDefaultValue.ShouldBeTrue($"{contract.Name}.{name}'s token must be defaulted");
			}
		}
	}

	[Fact]
	void Every_train_3_record_declares_the_ruled_member_orders()
	{
		(Type Record, string[] Members)[] records =
		[
			(typeof(AuthenticatorSetupResponse), ["SharedKey", "AuthenticatorUri", "QrCodeSvg"]),
			(typeof(EnableAuthenticatorRequest), ["Code"]),
			(typeof(RevealRecoveryCodesRequest), ["Handoff"]),
			(typeof(RecoveryCodesResponse), ["RecoveryCodes"]),
			(typeof(PasskeysResponse), ["Passkeys"]),
			(typeof(PasskeyEntry), ["CredentialId", "Name", "RenameUrl"]),
			(typeof(GetPasskeyRequest), ["CredentialId"]),
			(typeof(PasskeyCreationOptionsResponse), ["OptionsJson", "AttestationState"]),
			(typeof(AddPasskeyRequest), ["CredentialJson", "AttestationState"]),
			(typeof(RenamePasskeyRequest), ["CredentialId", "Name"]),
			(typeof(RemovePasskeyRequest), ["CredentialId"]),
			(typeof(PasskeyRequestOptionsResponse), ["OptionsJson", "AssertionState"]),
			(typeof(PasskeySignInRequest), ["CredentialJson", "AssertionState", "RememberMe", "ReturnUrl"]),
			(typeof(GetSignInMethodsRequest), ["ReturnUrl"]),
			(typeof(SignInMethodsResponse), ["PasswordSignInUrl", "PasskeySignInUrl", "Providers"]),
			(typeof(ExternalProviderLink), ["DisplayName", "StartUrl"])
		];

		foreach (var (record, members) in records)
		{
			record.GetCustomAttribute<DataContractAttribute>().ShouldNotBeNull(record.Name);
			record.GetProperties()
				.Select(p => (p.Name, p.GetCustomAttribute<DataMemberAttribute>()?.Order))
				.OrderBy(m => m.Order)
				.ShouldBe(members.Select((name, index) => (name, (int?)(index + 1))), record.Name);
		}
	}

	/// <summary>
	///     Guard (green before and after Task 3.1): Train 3 adds contracts rather than growing
	///     <see cref="IAccountService" /> (T3-20), so its eight operations stay exactly as Train 2 shipped them.
	/// </summary>
	[Fact]
	void IAccountService_is_unchanged()
	{
		typeof(IAccountService).GetMethods().Select(m => m.Name).ShouldBe(
			[
				nameof(IAccountService.GetAccountStatus), nameof(IAccountService.SetPhoneNumber),
				nameof(IAccountService.ChangePassword), nameof(IAccountService.SetPassword),
				nameof(IAccountService.ChangeEmail), nameof(IAccountService.GetExternalLogins),
				nameof(IAccountService.RemoveExternalLogin), nameof(IAccountService.DeletePersonalData)
			],
			ignoreOrder: true);
	}

	/// <summary>
	///     <see cref="LoginRequest" /> is pinned by reflection rather than a round trip (pre-flight
	///     ruling T1): its <c>Result&lt;EmailAddress&gt;</c> member is serialized only by Midgard's
	///     <c>ResultSerializers</c>, which this project does not reference.
	/// </summary>
	[Fact]
	void Login_request_return_url_is_wire_order_4_and_defaults_to_empty()
	{
		typeof(LoginRequest).GetProperty(nameof(LoginRequest.ReturnUrl)).ShouldNotBeNull()
			.GetCustomAttribute<DataMemberAttribute>().ShouldNotBeNull().Order.ShouldBe(4);
		new LoginRequest { EmailInput = "" }.ReturnUrl.ShouldBe(string.Empty);
	}

	[Fact]
	void Passkeys_response_round_trips_two_entries()
	{
		PasskeysResponse response = new()
		{
			Passkeys =
			[
				new() { CredentialId = "AQID", Name = "Laptop", RenameUrl = "/Account/Manage/RenamePasskey/AQID" },
				new() { CredentialId = "BAUG", Name = "", RenameUrl = "/Account/Manage/RenamePasskey/BAUG" }
			]
		};

		var clone = Serializer.DeepClone(response);

		clone.Passkeys.Length.ShouldBe(2);
		clone.Passkeys[0].ShouldBe(response.Passkeys[0]);
		clone.Passkeys[1].ShouldBe(response.Passkeys[1]);
	}

	[Fact]
	void An_empty_recovery_codes_response_round_trips_as_empty()
	{
		var clone = Serializer.DeepClone(new RecoveryCodesResponse());

		clone.RecoveryCodes.ShouldNotBeNull().ShouldBeEmpty();
	}

	[Fact]
	void A_full_passkey_sign_in_request_round_trips()
	{
		PasskeySignInRequest request = new()
		{
			CredentialJson = """{"id":"AQID"}""",
			AssertionState = "protected-state",
			RememberMe = true,
			ReturnUrl = "/Account/Manage"
		};

		Serializer.DeepClone(request).ShouldBe(request);
	}

	[Fact]
	void Sign_in_methods_response_round_trips_with_one_provider()
	{
		SignInMethodsResponse response = new()
		{
			PasswordSignInUrl = "/Account/Login?returnUrl=%2F",
			PasskeySignInUrl = "/Account/LoginWithPasskey?returnUrl=%2F",
			Providers = [new() { DisplayName = "GitHub", StartUrl = "/Account/PerformExternalLogin?provider=GitHub" }]
		};

		var clone = Serializer.DeepClone(response);

		clone.PasswordSignInUrl.ShouldBe(response.PasswordSignInUrl);
		clone.PasskeySignInUrl.ShouldBe(response.PasskeySignInUrl);
		clone.Providers.ShouldHaveSingleItem().ShouldBe(response.Providers[0]);
	}

	[Fact]
	void Sign_in_methods_response_round_trips_with_no_providers()
	{
		SignInMethodsResponse response = new() { PasswordSignInUrl = "/Account/Login", PasskeySignInUrl = "/Account/LoginWithPasskey" };

		var clone = Serializer.DeepClone(response);

		clone.PasswordSignInUrl.ShouldBe(response.PasswordSignInUrl);
		clone.PasskeySignInUrl.ShouldBe(response.PasskeySignInUrl);
		clone.Providers.ShouldNotBeNull().ShouldBeEmpty();
	}
}
