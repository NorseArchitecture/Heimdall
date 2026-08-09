using System.ServiceModel;
using Norse.Abstractions.Contracts;

namespace Norse.AuthN.Services;

/// <summary>
///     Issuance surface — real, network-callable gRPC methods that mint or clear the authenticated
///     cookie. The <c>CancellationToken</c> parameter rides every method so components can cancel
///     operations without a gateway wrapper.
///     Every request/response type here is a pure <c>[DataContract]</c> wire shape — no mediator
///     marker, no <c>[Authorize]</c>. Authorization policy (<see cref="AuthNPolicies.Public" />) and
///     mediator identity live entirely server-side, on Himinbjörg's <c>LoginCommand</c>/
///     <c>RegisterCommand</c>/<c>LogoutCommand</c> wrappers — this assembly never references
///     <c>Abstractions.Web.Server</c>, keeping the WASM footprint lean. The concrete
///     <c>AuthenticationService</c> implementation mirrors <see cref="AuthNPolicies.Public" /> on its own
///     methods purely for gRPC endpoint metadata; that mirror is the only place the policy name touches
///     the wire tier at all.
///     Every method returns <see cref="Outcome{T}" /> directly (spec §9, 2026-07-24 amendment to decided
///     law item 3) — the envelope <em>is</em> the wire method signature. Nothing in-process throws to
///     communicate a business failure; the one throw point in the whole chain is the gRPC server
///     interceptor (Midgard), pattern-matching the returned <see cref="Outcome{T}" /> at the transport
///     boundary, never here.
/// </summary>
[ServiceContract(Name = "grpc.authentication.v1.AuthenticationService")]
public interface IAuthenticationService
{
	/// <summary>Authenticates a user with the provided credentials.</summary>
	[OperationContract]
	Task<Outcome<NavigationResult>> Login(LoginRequest request, CancellationToken cancellationToken = default);

	/// <summary>Registers a new user account with the provided credentials.</summary>
	[OperationContract]
	Task<Outcome<NavigationResult>> Register(RegisterRequest request, CancellationToken cancellationToken = default);

	/// <summary>Reports whether an account already exists for <paramref name="request" />'s email.</summary>
	[OperationContract]
	Task<Outcome<BoolResponse>> EmailExists(EmailExistsRequest request, CancellationToken cancellationToken = default);

	/// <summary>
	///     Logs out the currently authenticated user. No request DTO — the caller's authenticated cookie
	///     identifies who's logging out, and protobuf-net.Grpc supports a <see cref="CancellationToken" />-only
	///     operation contract end to end (spike-verified: real <c>TestServer</c>, real HTTP/2, real
	///     <c>CreateGrpcService&lt;T&gt;</c> client proxy) — an empty request record would have carried no
	///     information at all.
	/// </summary>
	[OperationContract]
	Task<Outcome<NavigationResult>> Logout(CancellationToken cancellationToken = default);
}
