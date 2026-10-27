using Bunit;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using Norse.Abstractions.Contracts;
using Norse.AuthN.Components.FluentUI.Manage;
using Norse.AuthN.Services;

namespace Norse.AuthN.Components.FluentUI.Tests.Manage;

public sealed class EmailTests : BunitContext
{
	readonly IAccountService _account = Substitute.For<IAccountService>();
	readonly IIdentityService _identity = Substitute.For<IIdentityService>();

	public EmailTests()
	{
		Services.AddFluentUIComponents();
		// FluentUI components make JS interop calls bunit has no way to know about in advance —
		// loose mode is bunit's own documented answer (same rationale as LoginTests).
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services
			.AddSingleton(_account)
			.AddSingleton(_identity)
			.AddSingleton(Substitute.For<IAuthenticationService>())
			.AddScoped<IValidator<ChangeEmailRequest>, ChangeEmailRequestValidator>();
	}

	static AccountStatusResponse Status(bool emailConfirmed) =>
		new()
		{
			HasPassword = true,
			EmailConfirmed = emailConfirmed,
			TwoFactorEnabled = false,
			HasAuthenticator = false,
			RecoveryCodesLeft = 0,
			IsMachineRemembered = false,
			PasskeyCount = 0,
			ExternalLoginCount = 0,
		};

	[Fact]
	void Loads_status_then_profile_and_renders_the_form()
	{
		_account.GetAccountStatus(Arg.Any<CancellationToken>())
			.Returns(Outcome<AccountStatusResponse>.Ok(Status(emailConfirmed: true)));
		_identity.GetMyPersonalDataAsync(Arg.Any<GetMyPersonalDataRequest>(), Arg.Any<CancellationToken>())
			.Returns(Outcome<PersonalDataResponse>.Ok(new() { Email = "user@example.com", PhoneNumber = "" }));

		var page = Render<Email>();

		Received.InOrder(() =>
		{
			_account.GetAccountStatus(Arg.Any<CancellationToken>());
			_identity.GetMyPersonalDataAsync(Arg.Any<GetMyPersonalDataRequest>(), Arg.Any<CancellationToken>());
		});
		// FormName renders nothing under bUnit, so the markup is no witness — the rendered form's own
		// parameters are (Task 1.4 lesson).
		var form = page.FindComponent<ChangeEmailForm>().Instance;
		form.Email.ShouldBe("user@example.com");
		form.EmailConfirmed.ShouldBeTrue();
	}

	[Fact]
	void A_failed_status_load_renders_the_problem_and_no_form()
	{
		_account.GetAccountStatus(Arg.Any<CancellationToken>())
			.Returns(Outcome<AccountStatusResponse>.Err(ErrorCategory.Fault, correlationId: Guid.Empty));

		var page = Render<Email>();

		page.Markup.ShouldContain("Your email settings could not be loaded.");
		page.FindComponents<ChangeEmailForm>().ShouldBeEmpty();
		_identity.DidNotReceiveWithAnyArgs().GetMyPersonalDataAsync(default!, Xunit.TestContext.Current.CancellationToken);
	}

	[Fact]
	void A_failed_profile_load_renders_the_problem_and_no_form()
	{
		_account.GetAccountStatus(Arg.Any<CancellationToken>())
			.Returns(Outcome<AccountStatusResponse>.Ok(Status(emailConfirmed: false)));
		_identity.GetMyPersonalDataAsync(Arg.Any<GetMyPersonalDataRequest>(), Arg.Any<CancellationToken>())
			.Returns(Outcome<PersonalDataResponse>.Err(ErrorCategory.Fault, correlationId: Guid.Empty));

		var page = Render<Email>();

		page.Markup.ShouldContain("Your email settings could not be loaded.");
		page.FindComponents<ChangeEmailForm>().ShouldBeEmpty();
	}
}
