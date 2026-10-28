using System.ServiceModel;
using Norse.Abstractions.Contracts;

namespace Norse.AuthN.Services;

/// <summary>
///     The anonymous sign-in-method surface — the links between the sign-in pages and passkey sign-in
///     (spec <c>Glitnir/docs/Himinbjorg/specs/2026-10-26-identity-account-lifecycle-over-grpc-design.md</c>
///     §4.3 as amended). Public tier (<c>AuthNPolicies.Public</c>); authorization lives server-side on
///     Himinbjörg's command wrappers. Every request/response type is a pure <c>[DataContract]</c> wire
///     shape. The server authors every link and every <c>NextUrl</c>; a client-supplied return URL is
///     carried through, never navigated to directly.
/// </summary>
[ServiceContract(Name = "grpc.signinmethods.v1.SignInMethodsService")]
public interface ISignInMethodsService
{
	/// <summary>
	///     Returns the links to every sign-in method, each carrying the return URL. Continuation: in
	///     place (rendered payload).
	/// </summary>
	[OperationContract]
	Task<Outcome<SignInMethodsResponse>> GetSignInMethods(GetSignInMethodsRequest request, CancellationToken cancellationToken = default);

	/// <summary>
	///     Begins passkey sign-in: the WebAuthn request options and the protected, one-shot assertion
	///     state. Request-less. Continuation: in place (handed to the browser ceremony).
	/// </summary>
	[OperationContract]
	Task<Outcome<PasskeyRequestOptionsResponse>> GetPasskeyRequestOptions(CancellationToken cancellationToken = default);

	/// <summary>
	///     Completes passkey sign-in. Continuation: <c>ISessionTransition.Begin(result)</c> — a principal
	///     transition.
	/// </summary>
	[OperationContract]
	Task<Outcome<NavigationResult>> PasskeySignIn(PasskeySignInRequest request, CancellationToken cancellationToken = default);
}
