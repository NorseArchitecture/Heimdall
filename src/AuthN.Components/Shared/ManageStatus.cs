using System.Diagnostics.CodeAnalysis;

// CA1716 (identifiers should not match keywords reserved in other CLS languages): VB.NET reserves "Shared",
// and this namespace is fixed by the folder StatusMessage.razor already occupies — Razor-generated code is
// never analyzed, so ManageStatus is merely the first hand-written type to surface it. Scoped to this one
// namespace, never the project.
[assembly: SuppressMessage("Naming", "CA1716:Identifiers should not conflict with keywords", Scope = "namespace",
	Target = "~N:Norse.AuthN.Components.Shared",
	Justification = "Namespace mirrors the existing Shared/ folder that StatusMessage.razor occupies.")]

namespace Norse.AuthN.Components.Shared;

/// <summary>
///     Maps the <c>status</c> query key a Manage page redirects with onto the message the Manage layout
///     displays. The keys are the only thing that crosses the redirect — the text never does, so a
///     crafted query string can select a known message but never inject one.
/// </summary>
public static class ManageStatus
{
	/// <summary>The password was changed.</summary>
	public const string PasswordChanged = "password-changed";

	/// <summary>A password was set on an account that had none.</summary>
	public const string PasswordSet = "password-set";

	/// <summary>The phone number (the profile) was updated.</summary>
	public const string PhoneUpdated = "phone-updated";

	/// <summary>The email address was changed.</summary>
	public const string EmailChanged = "email-changed";

	/// <summary>An external login was removed.</summary>
	public const string ExternalLoginRemoved = "external-login-removed";

	/// <summary>An external login was added.</summary>
	public const string ExternalLoginAdded = "external-login-added";

	/// <summary>An external login could not be added, because it is already associated with an account.</summary>
	public const string ExternalLoginNotAdded = "external-login-not-added";

	/// <summary>The authenticator app key was reset.</summary>
	public const string AuthenticatorReset = "authenticator-reset";

	/// <summary>The authenticator app was verified, enabling two-factor authentication.</summary>
	public const string AuthenticatorVerified = "authenticator-verified";

	/// <summary>Two-factor authentication was disabled.</summary>
	public const string TwoFactorDisabled = "two-factor-disabled";

	/// <summary>The current browser was forgotten, so it is no longer remembered for two-factor sign-in.</summary>
	public const string BrowserForgotten = "browser-forgotten";

	/// <summary>A passkey was renamed.</summary>
	public const string PasskeyUpdated = "passkey-updated";

	/// <summary>An external login could not be added, for any definite reason other than an existing association.</summary>
	public const string ExternalLoginFailed = "external-login-failed";

	/// <summary>Two-factor authentication was enabled, but its recovery codes could not be confirmed.</summary>
	public const string RecoveryCodesFailed = "recovery-codes-failed";

	/// <summary>Resolves a status key to its display text.</summary>
	/// <param name="key">The <c>status</c> query value; matched ordinally.</param>
	/// <returns>The display text, or <see langword="null" /> for a null, empty or unknown key.</returns>
	public static string? Text(string? key) =>
		key switch
		{
			PasswordChanged => "Your password has been changed.",
			PasswordSet => "Your password has been set.",
			PhoneUpdated => "Your profile has been updated.",
			EmailChanged => "Your email has been changed.",
			ExternalLoginRemoved => "The external login was removed.",
			ExternalLoginAdded => "The external login was added.",
			ExternalLoginNotAdded => "Error: The external login was not added. External logins can only be associated with one account.",
			AuthenticatorReset => "Your authenticator app key has been reset, you will need to configure your authenticator app using the new key.",
			AuthenticatorVerified => "Your authenticator app has been verified.",
			TwoFactorDisabled => "Two-factor authentication has been disabled. You can re-enable it when you set up an authenticator app.",
			BrowserForgotten => "The current browser has been forgotten. When you log in again from this browser you will be prompted for your two-factor code.",
			PasskeyUpdated => "Passkey updated successfully.",
			ExternalLoginFailed => "Error: The external login could not be added. Try again.",
			RecoveryCodesFailed => "Two-factor authentication is on, but your recovery codes could not be confirmed. Generate new recovery codes now.",
			_ => null
		};
}
