using System.ServiceModel;
using Norse.Abstractions.Contracts;

namespace Norse.AuthN.Services;

/// <summary>
///     The caller's two-factor surface — authenticator setup, recovery codes, and the remembered
///     machine (spec <c>Glitnir/docs/Himinbjorg/specs/2026-10-26-identity-account-lifecycle-over-grpc-design.md</c>
///     §4.3 as amended). Account tier (<c>AuthNPolicies.Account</c>), like <see cref="IAccountService" />:
///     the tier is one policy spread across several contracts, and authorization lives server-side on
///     Himinbjörg's command wrappers. There is no subject-id parameter anywhere — the principal is the
///     only subject. Every request/response type is a pure <c>[DataContract]</c> wire shape. Continuations
///     (§6.3 as amended): a stamp-rotating or principal-changing operation returns
///     <see cref="Outcome{T}" /> of <see cref="NavigationResult" /> and the client calls
///     <c>ISessionTransition.Begin(result)</c>; the server authors every <c>NextUrl</c>.
/// </summary>
[ServiceContract(Name = "grpc.twofactor.v1.TwoFactorService")]
public interface ITwoFactorService
{
	/// <summary>
	///     Returns the caller's authenticator key, URI, and QR code, provisioning a missing key without
	///     rotating the security stamp. Request-less. Continuation: in place (rendered payload).
	/// </summary>
	[OperationContract]
	Task<Outcome<AuthenticatorSetupResponse>> GetAuthenticatorSetup(CancellationToken cancellationToken = default);

	/// <summary>
	///     Verifies an authenticator code and enables two-factor authentication. Rotates the security
	///     stamp; new recovery codes ride a one-shot handoff on the <c>NextUrl</c>. Continuation:
	///     <c>ISessionTransition.Begin(result)</c>.
	/// </summary>
	[OperationContract]
	Task<Outcome<NavigationResult>> EnableAuthenticator(EnableAuthenticatorRequest request, CancellationToken cancellationToken = default);

	/// <summary>
	///     Redeems a one-shot recovery-code handoff and returns the codes it carries. Continuation: in
	///     place (rendered payload).
	/// </summary>
	[OperationContract]
	Task<Outcome<RecoveryCodesResponse>> RevealRecoveryCodes(RevealRecoveryCodesRequest request, CancellationToken cancellationToken = default);

	/// <summary>
	///     Replaces the authenticator key and disables two-factor authentication until it is set up
	///     again. Rotates the security stamp. Request-less. Continuation: <c>ISessionTransition.Begin(result)</c>.
	/// </summary>
	[OperationContract]
	Task<Outcome<NavigationResult>> ResetAuthenticator(CancellationToken cancellationToken = default);

	/// <summary>
	///     Disables two-factor authentication. Rotates the security stamp. Request-less. Continuation:
	///     <c>ISessionTransition.Begin(result)</c>.
	/// </summary>
	[OperationContract]
	Task<Outcome<NavigationResult>> DisableTwoFactor(CancellationToken cancellationToken = default);

	/// <summary>
	///     Replaces the caller's recovery codes with a fresh set and returns them. Request-less.
	///     Continuation: in place (rendered payload).
	/// </summary>
	[OperationContract]
	Task<Outcome<RecoveryCodesResponse>> GenerateRecoveryCodes(CancellationToken cancellationToken = default);

	/// <summary>
	///     Forgets the calling machine, so the second factor is asked for again. The cookie delete rides
	///     a deferred completion on a circuit. Request-less. Continuation: <c>ISessionTransition.Begin(result)</c>.
	/// </summary>
	[OperationContract]
	Task<Outcome<NavigationResult>> ForgetTwoFactorClient(CancellationToken cancellationToken = default);
}
