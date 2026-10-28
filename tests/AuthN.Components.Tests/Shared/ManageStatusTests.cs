using System.Diagnostics.CodeAnalysis;
using Norse.AuthN.Components.Shared;

// CA1716: VB.NET reserves "Shared"; the namespace mirrors the Shared/ folder under test (same ruling as
// ManageStatus.cs itself), scoped to this one namespace.
[assembly: SuppressMessage("Naming", "CA1716:Identifiers should not conflict with keywords", Scope = "namespace",
	Target = "~N:Norse.AuthN.Components.Tests.Shared",
	Justification = "Namespace mirrors the Shared/ folder of the subject under test.")]

namespace Norse.AuthN.Components.Tests.Shared;

public sealed class ManageStatusTests
{
	[Theory]
	[InlineData(ManageStatus.PasswordChanged, "Your password has been changed.")]
	[InlineData(ManageStatus.PasswordSet, "Your password has been set.")]
	[InlineData(ManageStatus.PhoneUpdated, "Your profile has been updated.")]
	[InlineData(ManageStatus.EmailChanged, "Your email has been changed.")]
	[InlineData(ManageStatus.ExternalLoginRemoved, "The external login was removed.")]
	[InlineData(ManageStatus.ExternalLoginAdded, "The external login was added.")]
	[InlineData(ManageStatus.ExternalLoginNotAdded, "Error: The external login was not added. External logins can only be associated with one account.")]
	[InlineData(ManageStatus.AuthenticatorReset, "Your authenticator app key has been reset, you will need to configure your authenticator app using the new key.")]
	[InlineData(ManageStatus.AuthenticatorVerified, "Your authenticator app has been verified.")]
	[InlineData(ManageStatus.TwoFactorDisabled, "Two-factor authentication has been disabled. You can re-enable it when you set up an authenticator app.")]
	[InlineData(ManageStatus.BrowserForgotten, "The current browser has been forgotten. When you log in again from this browser you will be prompted for your two-factor code.")]
	[InlineData(ManageStatus.PasskeyUpdated, "Passkey updated successfully.")]
	[InlineData(ManageStatus.ExternalLoginFailed, "Error: The external login could not be added. Try again.")]
	[InlineData(ManageStatus.RecoveryCodesFailed, "Two-factor authentication is on, but your recovery codes could not be confirmed. Generate new recovery codes now.")]
	void A_known_key_maps_to_its_text(string key, string text) =>
		ManageStatus.Text(key).ShouldBe(text);

	// The keys are wire-visible (they ride the server's redirect), so their spelling is a contract too.
	[Theory]
	[InlineData(ManageStatus.AuthenticatorVerified, "authenticator-verified")]
	[InlineData(ManageStatus.TwoFactorDisabled, "two-factor-disabled")]
	[InlineData(ManageStatus.BrowserForgotten, "browser-forgotten")]
	[InlineData(ManageStatus.PasskeyUpdated, "passkey-updated")]
	[InlineData(ManageStatus.ExternalLoginFailed, "external-login-failed")]
	[InlineData(ManageStatus.RecoveryCodesFailed, "recovery-codes-failed")]
	void A_train_3_key_has_its_wire_spelling(string key, string spelling) =>
		key.ShouldBe(spelling);

	[Theory]
	[InlineData("nope")]
	[InlineData("")]
	[InlineData(null)]
	void An_unknown_key_maps_to_null(string? key) =>
		ManageStatus.Text(key).ShouldBeNull();
}
