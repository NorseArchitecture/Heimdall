using Bunit;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using Norse.Abstractions.Contracts;
using Norse.AuthN.Components.FluentUI.Manage;
using Norse.AuthN.Services;

namespace Norse.AuthN.Components.FluentUI.Tests.Manage;

public sealed class DeletePersonalDataTests : BunitContext
{
	readonly IAccountService _account = Substitute.For<IAccountService>();

	public DeletePersonalDataTests()
	{
		Services.AddFluentUIComponents();
		// FluentUI components make JS interop calls bunit has no way to know about in advance —
		// loose mode is bunit's own documented answer (same rationale as LoginTests).
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services
			.AddSingleton(_account)
			.AddSingleton(Substitute.For<ISessionTransition>())
			.AddScoped<IValidator<DeletePersonalDataRequest>, DeletePersonalDataRequestValidator>();
	}

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

	IRenderedComponent<DeletePersonalData> RenderWith(Outcome<AccountStatusResponse> status)
	{
		_account.GetAccountStatus(Arg.Any<CancellationToken>()).Returns(status);
		return Render<DeletePersonalData>();
	}

	// FormName renders nothing under bUnit, so the markup is no witness to which form rendered or what it
	// carries — the rendered component tree is (Task 1.4 lesson).
	[Fact]
	void Renders_the_form_with_a_password_field_when_a_password_exists()
	{
		var page = RenderWith(Outcome<AccountStatusResponse>.Ok(Status(hasPassword: true)));

		var form = page.FindComponents<DeletePersonalDataForm>().ShouldHaveSingleItem();
		form.Instance.HasPassword.ShouldBeTrue();
		form.FindComponents<FluentTextInput>().ShouldHaveSingleItem();
	}

	[Fact]
	void Renders_the_form_without_a_password_field_otherwise()
	{
		var page = RenderWith(Outcome<AccountStatusResponse>.Ok(Status(hasPassword: false)));

		var form = page.FindComponents<DeletePersonalDataForm>().ShouldHaveSingleItem();
		form.Instance.HasPassword.ShouldBeFalse();
		form.FindComponents<FluentTextInput>().ShouldBeEmpty();
		form.FindAll("fluent-text-input").ShouldBeEmpty();
	}
}
