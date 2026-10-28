using Bunit;
using Bunit.TestDoubles;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using Norse.Abstractions.Contracts;
using Norse.AuthN.Components.FluentUI.Manage;
using Norse.AuthN.Services;

namespace Norse.AuthN.Components.FluentUI.Tests.Manage;

public sealed class ChangePasswordTests : InteractiveBunitContext
{
	readonly IAccountService _account = Substitute.For<IAccountService>();

	public ChangePasswordTests()
	{
		Services.AddFluentUIComponents();
		// FluentUI components make JS interop calls bunit has no way to know about in advance —
		// loose mode is bunit's own documented answer (same rationale as LoginTests).
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services
			.AddSingleton(_account)
			.AddSingleton(Substitute.For<ISessionTransition>())
			.AddScoped<IValidator<ChangePasswordRequest>, ChangePasswordRequestValidator>()
			.AddScoped<IValidator<SetPasswordRequest>, SetPasswordRequestValidator>();
	}

	BunitNavigationManager Navigation => Services.GetRequiredService<BunitNavigationManager>();

	static AccountStatusResponse Status(bool hasPassword) =>
		new()
		{
			HasPassword = hasPassword,
			EmailConfirmed = true,
			TwoFactorEnabled = false,
			HasAuthenticator = false,
			RecoveryCodesLeft = 0,
			IsMachineRemembered = false,
			PasskeyCount = 0,
			ExternalLoginCount = 0,
		};

	IRenderedComponent<ChangePassword> RenderWith(Outcome<AccountStatusResponse> status)
	{
		_account.GetAccountStatus(Arg.Any<CancellationToken>()).Returns(status);
		return Render<ChangePassword>();
	}

	// FormName renders nothing under bUnit, so the markup is no witness to which form rendered — the
	// rendered component tree is (Task 1.4 lesson).
	[Fact]
	void Renders_the_change_form_when_a_password_exists()
	{
		var page = RenderWith(Outcome<AccountStatusResponse>.Ok(Status(hasPassword: true)));

		page.FindComponents<ChangePasswordForm>().ShouldHaveSingleItem();
		page.FindComponents<SetPasswordForm>().ShouldBeEmpty();
		page.Markup.ShouldNotContain("You do not have a local username/password for this site.");
		page.Find("h3").TextContent.ShouldBe("Change password");
		Navigation.History.ShouldBeEmpty();
	}

	[Fact]
	void Renders_the_set_form_when_no_password_exists()
	{
		var page = RenderWith(Outcome<AccountStatusResponse>.Ok(Status(hasPassword: false)));

		page.FindComponents<SetPasswordForm>().ShouldHaveSingleItem();
		page.FindComponents<ChangePasswordForm>().ShouldBeEmpty();
		page.Markup.ShouldContain(
			"You do not have a local username/password for this site. Add a local account so you can log in without an external login.");
		page.Find("h3").TextContent.ShouldBe("Set password");
		Navigation.History.ShouldBeEmpty();
	}

	[Fact]
	void A_failed_status_load_renders_the_problem()
	{
		var page = RenderWith(Outcome<AccountStatusResponse>.Err(ErrorCategory.Fault, correlationId: Guid.Empty));

		page.Markup.ShouldContain("Your password settings could not be loaded.");
		page.FindComponents<ChangePasswordForm>().ShouldBeEmpty();
		page.FindComponents<SetPasswordForm>().ShouldBeEmpty();
	}
}
