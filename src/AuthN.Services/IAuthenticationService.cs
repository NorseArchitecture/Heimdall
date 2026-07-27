using System.ServiceModel;
using Microsoft.AspNetCore.Authorization;
using Norse.Abstractions.Contracts;

namespace Norse.AuthN.Services;

/// <summary>
/// Issuance surface — real, network-callable gRPC methods that mint or clear the authenticated
/// cookie. The <c>CancellationToken</c> parameter rides the contract so components can cancel
/// operations without a gateway wrapper. <see cref="AuthNPolicies.Public"/> on every method is a real,
/// explicit declaration (decided law item 4), not an unprotected surface — it just imposes no
/// requirement beyond "some principal exists," which every request already has.
///
/// Every method returns <see cref="Outcome{T}"/> directly (spec §9, 2026-07-24 amendment to decided
/// law item 3) — the envelope <em>is</em> the wire method signature. Nothing in-process throws to
/// communicate a business failure; the one throw point in the whole chain is the gRPC server
/// interceptor (Midgard), pattern-matching the returned <see cref="Outcome{T}"/> at the transport
/// boundary, never here.
/// </summary>
[ServiceContract(Name = "grpc.authentication.v1.AuthenticationService")]
public interface IAuthenticationService
{
	/// <summary>Authenticates a user with the provided credentials.</summary>
	[Authorize(Policy = AuthNPolicies.Public)]
	[OperationContract]
	Task<Outcome<LoginResult>> Login(LoginRequest request, CancellationToken cancellationToken = default);

	/// <summary>Registers a new user account with the provided credentials.</summary>
	[Authorize(Policy = AuthNPolicies.Public)]
	[OperationContract]
	Task<Outcome<Unit>> Register(RegisterRequest request, CancellationToken cancellationToken = default);

	/// <summary>Logs out the currently authenticated user.</summary>
	[Authorize(Policy = AuthNPolicies.Public)]
	[OperationContract]
	Task<Outcome<LogoutResult>> Logout(LogoutRequest request, CancellationToken cancellationToken = default);
}
