using System.ServiceModel;
using Norse.Abstractions.Contracts;

namespace Norse.AuthN.Services;

/// <summary>
/// Issuance surface — real, network-callable gRPC methods that mint or clear the authenticated
/// cookie. The <c>CancellationToken</c> parameter rides the contract so components can cancel
/// operations without a gateway wrapper. Authorization policy <see cref="AuthNPolicies.Public"/> is
/// declared on the request records <see cref="LoginRequest"/>, <see cref="RegisterRequest"/>, and
/// <see cref="LogoutRequest"/> (read by the mediator's <c>AuthorizationBehavior</c>) and mirrored on
/// the concrete <c>AuthenticationService</c> class for gRPC endpoint metadata — the interface itself
/// declares no policy.
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
	[OperationContract]
	Task<Outcome<LoginResult>> Login(LoginRequest request, CancellationToken cancellationToken = default);

	/// <summary>Registers a new user account with the provided credentials.</summary>
	[OperationContract]
	Task<Outcome<Unit>> Register(RegisterRequest request, CancellationToken cancellationToken = default);

	/// <summary>Logs out the currently authenticated user.</summary>
	[OperationContract]
	Task<Outcome<LogoutResult>> Logout(LogoutRequest request, CancellationToken cancellationToken = default);
}
