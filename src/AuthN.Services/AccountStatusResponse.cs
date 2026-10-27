using System.Runtime.Serialization;

namespace Norse.AuthN.Services;

/// <summary>
///     The wire response for <see cref="IAccountService.GetAccountStatus" /> — the caller's account
///     state the account pages branch their rendering on (spec §4.3). Presentation facts only; every
///     guard they suggest is enforced again server-side by the operation it gates.
/// </summary>
[DataContract]
public sealed record AccountStatusResponse
{
	/// <summary>Whether the account has a local password.</summary>
	[DataMember(Order = 1)]
	public required bool HasPassword { get; init; }

	/// <summary>Whether the account's email address is confirmed.</summary>
	[DataMember(Order = 2)]
	public required bool EmailConfirmed { get; init; }

	/// <summary>Whether two-factor authentication is enabled.</summary>
	[DataMember(Order = 3)]
	public required bool TwoFactorEnabled { get; init; }

	/// <summary>Whether an authenticator key is configured.</summary>
	[DataMember(Order = 4)]
	public required bool HasAuthenticator { get; init; }

	/// <summary>The number of unredeemed recovery codes.</summary>
	[DataMember(Order = 5)]
	public required int RecoveryCodesLeft { get; init; }

	/// <summary>Whether the calling machine is remembered for two-factor purposes.</summary>
	[DataMember(Order = 6)]
	public required bool IsMachineRemembered { get; init; }

	/// <summary>The number of registered passkeys.</summary>
	[DataMember(Order = 7)]
	public required int PasskeyCount { get; init; }

	/// <summary>The number of linked external logins.</summary>
	[DataMember(Order = 8)]
	public required int ExternalLoginCount { get; init; }
}
