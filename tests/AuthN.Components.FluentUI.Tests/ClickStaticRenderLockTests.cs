using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using Norse.Abstractions.Components;
using Norse.Abstractions.Contracts;
using Norse.AuthN.Components.FluentUI.Manage;
using Norse.AuthN.Services;

namespace Norse.AuthN.Components.FluentUI.Tests;

// Derives from BunitContext, not InteractiveBunitContext: that base would overwrite the static
// renderer these facts are about. A prerendered click trigger a user can reach before Blazor boots
// (or for good, if the interactive runtime never starts) has no circuit behind it, so a live-looking
// button would read as broken. Every click trigger renders disabled until the page is interactive.
public sealed class ClickStaticRenderLockTests : BunitContext
{
	public ClickStaticRenderLockTests()
	{
		Services.AddFluentUIComponents();
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services
			.AddSingleton(Account())
			.AddSingleton(Substitute.For<IAuthenticationService>())
			.AddSingleton(Substitute.For<IIdentityService>())
			.AddSingleton(Substitute.For<ITwoFactorService>())
			.AddSingleton(Passkey())
			.AddSingleton(SignInMethods())
			.AddSingleton(Substitute.For<ISessionTransition>());
	}

	// ExternalLogins loads on render, static or not, and offers Remove only for a removable login;
	// TwoFactorAuthentication offers Forget this browser only for a remembered machine.
	static IAccountService Account()
	{
		var account = Substitute.For<IAccountService>();
		account.GetAccountStatus(Arg.Any<CancellationToken>())
			.Returns(Outcome<AccountStatusResponse>.Ok(new()
			{
				HasPassword = true,
				EmailConfirmed = true,
				TwoFactorEnabled = true,
				HasAuthenticator = true,
				RecoveryCodesLeft = 10,
				IsMachineRemembered = true,
				PasskeyCount = 0,
				ExternalLoginCount = 0,
			}));
		account.GetExternalLogins(Arg.Any<CancellationToken>())
			.Returns(Outcome<ExternalLoginsResponse>.Ok(new()
			{
				Logins = [new() { LoginProvider = "Google", ProviderKey = "g-1", DisplayName = "Google" }],
				CanRemove = true,
			}));
		return account;
	}

	// Passkeys loads on render, static or not, and offers Delete per listed passkey; Add renders below the cap,
	// disabled until browser support is known (a static render never learns it).
	static IPasskeyService Passkey()
	{
		var passkeys = Substitute.For<IPasskeyService>();
		passkeys.GetPasskeys(Arg.Any<CancellationToken>())
			.Returns(Outcome<PasskeysResponse>.Ok(new()
			{
				Passkeys = [new() { CredentialId = "a", Name = "Laptop", RenameUrl = "/Account/Manage/RenamePasskey/a" }],
			}));
		return passkeys;
	}

	// LoginWithPasskey renders SignInMethodLinks, which loads on render, static or not.
	static ISignInMethodsService SignInMethods()
	{
		var signInMethods = Substitute.For<ISignInMethodsService>();
		signInMethods.GetSignInMethods(Arg.Any<GetSignInMethodsRequest>(), Arg.Any<CancellationToken>())
			.Returns(Outcome<SignInMethodsResponse>.Ok(new()));
		return signInMethods;
	}

	// Every row renders with whichever of these parameters it declares, so adding a row is one line.
	static readonly Dictionary<string, object> _parameters = new()
	{
		["UserId"] = "u",
		["Code"] = "c",
		["Email"] = "user@example.com",
	};

	public static TheoryData<Type, string> Triggers =>
	new()
	{
		{ typeof(Logout), "button" },
		{ typeof(ConfirmEmailPanel), "fluent-button" },
		{ typeof(ConfirmEmailChangePanel), "fluent-button" },
		{ typeof(ExternalLogins), "fluent-button[data-provider]" },
		{ typeof(PersonalData), "fluent-button" },
		{ typeof(ChangeEmailForm), "fluent-button[type='button']" },
		{ typeof(TwoFactorAuthentication), "fluent-button" },
		{ typeof(ResetAuthenticator), "fluent-button" },
		{ typeof(Disable2fa), "fluent-button" },
		{ typeof(GenerateRecoveryCodes), "fluent-button" },
		// Static half only: a static render never learns support, so Add is disabled there either way. The
		// live-circuit CanAct and in-flight guards are pinned by PasskeysTests.Add_and_every_delete_are_disabled_while_an_add_is_in_flight.
		{ typeof(Passkeys), "fluent-button[data-action='add']" },
		{ typeof(Passkeys), "fluent-button[data-credential-id]" },
		// Static half only, as for Add: the live-circuit guards are pinned by
		// LoginWithPasskeyTests.The_button_is_disabled_while_the_ceremony_runs.
		{ typeof(LoginWithPasskey), "fluent-button[data-action='passkey']" },
	};

	// Outcome components with no click trigger: they only load (or only link), so there is nothing to lock.
	static readonly HashSet<Type> _loaderOnly =
	[
		typeof(FluentUI.Manage.Index),
		typeof(Email),
		typeof(ChangePassword),
		typeof(SetPassword),
		typeof(DeletePersonalData),
		typeof(SignInMethodLinks),
		typeof(EnableAuthenticator),
		typeof(RecoveryCodesHandoff),
		typeof(RenamePasskey),
	];

	// Guard: the theory is only as good as its list. A new outcome component with a click trigger that
	// never joins it is never locked, so every one must be a row or be declared loader-only.
	[Fact]
	void Every_outcome_component_with_a_click_trigger_is_in_the_theory()
	{
		var components = new[] { typeof(Logout).Assembly, typeof(GateLayout).Assembly }
			.SelectMany(assembly => assembly.GetTypes())
			.Where(t => t is { IsAbstract: false } && t.IsSubclassOf(typeof(OutcomeComponentBase)));
		var accounted = Triggers.Select(row => row.Data.Item1).Concat(_loaderOnly).ToHashSet();

		components.Where(t => !accounted.Contains(t)).ShouldBeEmpty();
	}

	[Theory]
	[MemberData(nameof(Triggers))]
	void A_statically_rendered_click_trigger_is_disabled(Type component, string selector)
	{
		SetRendererInfo(new("Static", false));

		var page = Render(builder =>
		{
			builder.OpenComponent(0, component);
			foreach (var (name, value) in _parameters)
				if (component.GetProperty(name)?.IsDefined(typeof(ParameterAttribute), true) is true)
					builder.AddAttribute(1, name, value);
			builder.CloseComponent();
		});

		page.Find(selector).HasAttribute("disabled").ShouldBeTrue(component.Name);
	}
}
