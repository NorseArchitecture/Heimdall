using System.ServiceModel;
using Norse.Abstractions.Contracts;

namespace Norse.AuthN.Services;

/// <summary>
///     The caller's passkey management surface — list, register, rename, remove (spec
///     <c>Glitnir/docs/Himinbjorg/specs/2026-10-26-identity-account-lifecycle-over-grpc-design.md</c>
///     §4.3 as amended, §5.5). Account tier (<c>AuthNPolicies.Account</c>), like <see cref="IAccountService" />;
///     authorization lives server-side on Himinbjörg's command wrappers. The registration ceremony is
///     stateless on the server: <see cref="GetPasskeyCreationOptions" /> hands out data-protected state
///     that <see cref="AddPasskey" /> echoes back. Every request/response type is a pure
///     <c>[DataContract]</c> wire shape. Continuations (§6.3 as amended): an operation that ends on
///     another page returns <see cref="Outcome{T}" /> of <see cref="NavigationResult" /> and the client
///     soft-navigates to the server's <c>NextUrl</c>.
/// </summary>
[ServiceContract(Name = "grpc.passkey.v1.PasskeyService")]
public interface IPasskeyService
{
	/// <summary>Lists the caller's passkeys. Request-less. Continuation: in place (rendered payload).</summary>
	[OperationContract]
	Task<Outcome<PasskeysResponse>> GetPasskeys(CancellationToken cancellationToken = default);

	/// <summary>
	///     Returns one of the caller's passkeys — the rename page's pre-fill. Continuation: in place
	///     (rendered payload).
	/// </summary>
	[OperationContract]
	Task<Outcome<PasskeyEntry>> GetPasskey(GetPasskeyRequest request, CancellationToken cancellationToken = default);

	/// <summary>
	///     Begins passkey registration: the WebAuthn creation options and the protected attestation
	///     state. Request-less. Continuation: in place (handed to the browser ceremony).
	/// </summary>
	[OperationContract]
	Task<Outcome<PasskeyCreationOptionsResponse>> GetPasskeyCreationOptions(CancellationToken cancellationToken = default);

	/// <summary>
	///     Completes passkey registration, bound to the account completing it. At the passkey cap the
	///     server answers <c>Conflict</c>. Continuation: soft <c>NavigationManager.NavigateTo(result.NextUrl)</c>
	///     (the new passkey's rename page).
	/// </summary>
	[OperationContract]
	Task<Outcome<NavigationResult>> AddPasskey(AddPasskeyRequest request, CancellationToken cancellationToken = default);

	/// <summary>
	///     Renames one of the caller's passkeys. Continuation: soft
	///     <c>NavigationManager.NavigateTo(result.NextUrl)</c>.
	/// </summary>
	[OperationContract]
	Task<Outcome<NavigationResult>> RenamePasskey(RenamePasskeyRequest request, CancellationToken cancellationToken = default);

	/// <summary>
	///     Removes one of the caller's passkeys. Acknowledgment only. Continuation: in place.
	/// </summary>
	[OperationContract]
	Task<Outcome<BoolResponse>> RemovePasskey(RemovePasskeyRequest request, CancellationToken cancellationToken = default);
}
