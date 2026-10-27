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
	void A_known_key_maps_to_its_text(string key, string text) =>
		ManageStatus.Text(key).ShouldBe(text);

	[Theory]
	[InlineData("nope")]
	[InlineData("")]
	[InlineData(null)]
	void An_unknown_key_maps_to_null(string? key) =>
		ManageStatus.Text(key).ShouldBeNull();
}
