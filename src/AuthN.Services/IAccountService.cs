using System.ServiceModel;
using Norse.Abstractions.Contracts;

namespace Norse.AuthN.Services;

/// <summary>
///     Self-service account surface — real, network-callable gRPC methods a signed-in (non-anonymous)
///     user calls to inspect and change their own account (spec
///     <c>Glitnir/docs/Himinbjorg/specs/2026-10-26-identity-account-lifecycle-over-grpc-design.md</c>
///     §4.3). There is no subject-id parameter anywhere: the principal is the only subject. The
///     <c>CancellationToken</c> parameter rides every method, same as <see cref="IAuthenticationService" />.
///     Every request/response type here is a pure <c>[DataContract]</c> wire shape — no mediator
///     marker, no <c>[Authorize]</c>. Authorization policy (the account tier,
///     <c>AuthNPolicies.Account</c>, spec §4.4) and mediator identity live entirely server-side, on Himinbjörg's command wrappers.
///     Return-type rule (§4.1): anything that refreshes the sign-in returns
///     <see cref="Outcome{T}" /> of <see cref="NavigationResult" /> (a deferred cookie write on a Blazor
///     Server circuit), acknowledgment-only operations return <see cref="BoolResponse" />, and data
///     operations return their own record.
/// </summary>
[ServiceContract(Name = "grpc.account.v1.AccountService")]
public interface IAccountService
{
	/// <summary>
	///     Returns the caller's account state — the facts the scaffold's <c>Manage/*.razor</c> pages each
	///     read from <c>UserManager</c> directly (<c>Manage/TwoFactorAuthentication.razor</c>,
	///     <c>Manage/Email.razor</c>, <c>Manage/ExternalLogins.razor</c>, …). Request-less (§4.3).
	/// </summary>
	[OperationContract]
	Task<Outcome<AccountStatusResponse>> GetAccountStatus(CancellationToken cancellationToken = default);

	/// <summary>
	///     Sets or clears the caller's phone number — replaces the scaffold's <c>Manage/Index.razor</c>
	///     (§4.3). Rotates the security stamp, so the sign-in is refreshed.
	/// </summary>
	[OperationContract]
	Task<Outcome<NavigationResult>> SetPhoneNumber(SetPhoneNumberRequest request, CancellationToken cancellationToken = default);

	/// <summary>
	///     Changes the caller's password — replaces the scaffold's <c>Manage/ChangePassword.razor</c>
	///     (§4.3). Rotates the security stamp, so the sign-in is refreshed.
	/// </summary>
	[OperationContract]
	Task<Outcome<NavigationResult>> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken = default);

	/// <summary>
	///     Adds a password to an account that has none — replaces the scaffold's
	///     <c>Manage/SetPassword.razor</c> (§4.3). An account that already has one answers <c>Conflict</c>.
	/// </summary>
	[OperationContract]
	Task<Outcome<NavigationResult>> SetPassword(SetPasswordRequest request, CancellationToken cancellationToken = default);

	/// <summary>
	///     Sends a change-confirmation link to a new email address — replaces the scaffold's
	///     <c>Manage/Email.razor</c> (§4.3). Acknowledgment only; an unchanged email answers <c>Validation</c>.
	/// </summary>
	[OperationContract]
	Task<Outcome<BoolResponse>> ChangeEmail(ChangeEmailRequest request, CancellationToken cancellationToken = default);

	/// <summary>
	///     Lists the caller's linked external logins and the providers still available to link — the
	///     read half of the scaffold's <c>Manage/ExternalLogins.razor</c> (§4.3). Request-less.
	/// </summary>
	[OperationContract]
	Task<Outcome<ExternalLoginsResponse>> GetExternalLogins(CancellationToken cancellationToken = default);

	/// <summary>
	///     Unlinks an external login — the remove half of the scaffold's <c>Manage/ExternalLogins.razor</c>
	///     (§4.3). Refused with <c>Conflict</c> when it would remove the account's last usable sign-in
	///     method (§5.9); the store's guard is the answer, not <see cref="ExternalLoginsResponse.CanRemove" />.
	/// </summary>
	[OperationContract]
	Task<Outcome<NavigationResult>> RemoveExternalLogin(RemoveExternalLoginRequest request, CancellationToken cancellationToken = default);

	/// <summary>
	///     Erases the caller's account — replaces the scaffold's <c>Manage/DeletePersonalData.razor</c>
	///     (§4.3, §5.6). The password is checked when the account has one; success signs the caller out.
	/// </summary>
	[OperationContract]
	Task<Outcome<NavigationResult>> DeletePersonalData(DeletePersonalDataRequest request, CancellationToken cancellationToken = default);
}
