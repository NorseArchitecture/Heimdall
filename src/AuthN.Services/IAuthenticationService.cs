using System.ServiceModel;
using Microsoft.AspNetCore.Authorization;
using Norse.Abstractions.Contracts;

namespace Norse.AuthN.Services;

/// <summary>
/// Issuance surface — real, network-callable gRPC methods that mint or clear the authenticated
/// cookie. No <c>CallContext</c> parameter, deliberately. <see cref="AuthNPolicies.Public"/> on every
/// method is a real, explicit declaration (decided law item 4), not an unprotected surface — it just
/// imposes no requirement beyond "some principal exists," which every request already has.
/// </summary>
[GenerateGateway]
[ServiceContract(Name = "grpc.authentication.v1.AuthenticationService")]
public interface IAuthenticationService
{
	/// <summary>Authenticates a user with the provided credentials.</summary>
	[Authorize(Policy = AuthNPolicies.Public)]
	[OperationContract]
	Task<LoginResult> Login(LoginRequest request);

	/// <summary>Registers a new user account with the provided credentials.</summary>
	[Authorize(Policy = AuthNPolicies.Public)]
	[OperationContract]
	Task Register(RegisterRequest request);

	/// <summary>Logs out the currently authenticated user.</summary>
	[Authorize(Policy = AuthNPolicies.Public)]
	[OperationContract]
	Task<LogoutResult> Logout(LogoutRequest request);
}
