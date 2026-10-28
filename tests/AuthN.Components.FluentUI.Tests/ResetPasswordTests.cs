using Bunit;
using Bunit.TestDoubles;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using Norse.AuthN.Services;

namespace Norse.AuthN.Components.FluentUI.Tests;

public sealed class ResetPasswordTests : InteractiveBunitContext
{
	public ResetPasswordTests()
	{
		Services.AddFluentUIComponents();
		// FluentUI components make JS interop calls bunit has no way to know about in advance —
		// loose mode is bunit's own documented answer (same rationale as LoginTests).
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddSingleton(Substitute.For<IAuthenticationService>());
		Services.AddScoped<IValidator<ResetPasswordRequest>, ResetPasswordRequestValidator>();
	}

	BunitNavigationManager Navigation => Services.GetRequiredService<BunitNavigationManager>();

	[Fact]
	void A_missing_code_redirects_softly_to_the_invalid_reset_page()
	{
		Navigation.NavigateTo("Account/ResetPassword");

		var page = Render<ResetPassword>();

		var entry = Navigation.History.First();
		entry.Uri.ShouldEndWith("Account/InvalidPasswordReset");
		entry.Options.ReplaceHistoryEntry.ShouldBeTrue();
		entry.Options.ForceLoad.ShouldBeFalse();
		page.FindComponents<ResetPasswordForm>().ShouldBeEmpty();
		page.Markup.Trim().ShouldBeEmpty();
	}

	[Fact]
	void A_present_code_is_handed_to_the_form()
	{
		Navigation.NavigateTo("Account/ResetPassword?code=C");

		var page = Render<ResetPassword>();

		page.FindComponent<ResetPasswordForm>().Instance.Code.ShouldBe("C");
		Navigation.Uri.ShouldBe(Navigation.ToAbsoluteUri("Account/ResetPassword?code=C").ToString());
	}
}
