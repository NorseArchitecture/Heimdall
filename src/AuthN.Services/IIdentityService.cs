using System.ServiceModel;
using Norse.Abstractions.Contracts;

namespace Norse.AuthN.Services;

/// <summary>
///     Disclosure surface — real, network-callable gRPC methods that return a subject's own or a
///     second party's personal data (spec:
///     <c>Glitnir/docs/Platform/specs/2026-08-03-pii-primitives-identity-erasure-seam-design.md</c>
///     §6). The <c>CancellationToken</c> parameter rides every method, same as <see cref="IAuthenticationService" />.
///     Every request/response type here is a pure <c>[DataContract]</c> wire shape — no mediator
///     marker, no <c>[Authorize]</c>. Authorization policy (<see cref="IdentityPolicies.Self" />/
///     <see cref="IdentityPolicies.MaskedDisclosure" />) and mediator identity live entirely
///     server-side, on Himinbjörg's command wrappers — this assembly never references
///     <c>Abstractions.Web.Server</c>, keeping the WASM footprint lean. The concrete
///     <c>IdentityService</c> implementation mirrors the policy constants on its own methods purely
///     for gRPC endpoint metadata; that mirror is the only place the policy name touches the wire
///     tier at all.
///     Every method returns <see cref="Outcome{T}" /> directly, matching <see cref="IAuthenticationService" />'s
///     law — the envelope <em>is</em> the wire method signature.
/// </summary>
[ServiceContract(Name = "grpc.identity.v1.IdentityService")]
public interface IIdentityService
{
	/// <summary>
	///     Returns the caller's own personal data, full and unmasked. There is no subject-id parameter —
	///     the principal's own identity is the only subject this method can ever disclose (spec §6.1:
	///     asking about someone else through this method is unrepresentable by construction).
	/// </summary>
	[OperationContract]
	Task<Outcome<PersonalDataResponse>> GetMyPersonalDataAsync(GetMyPersonalDataRequest request,
		CancellationToken cancellationToken = default);

	/// <summary>
	///     Returns a second party's personal data, masked. Reserved for callers with a declared, ratified
	///     need (spec §6: system role) — the endpoint chooses masked, it never authors a mask.
	/// </summary>
	[OperationContract]
	Task<Outcome<MaskedPersonalDataResponse>> GetMaskedPersonalDataAsync(GetMaskedPersonalDataRequest request,
		CancellationToken cancellationToken = default);
}
