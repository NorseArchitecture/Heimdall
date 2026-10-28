using Bunit;
using Bunit.TestDoubles;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using Norse.Abstractions.Contracts;
using Norse.AuthN.Services;

namespace Norse.AuthN.Components.FluentUI.Tests;

public sealed class LoginWith2faTests : InteractiveBunitContext
{
	readonly IAuthenticationService _service = Substitute.For<IAuthenticationService>();
	readonly ISessionTransition _sessionTransition = Substitute.For<ISessionTransition>();

	public LoginWith2faTests()
	{
		Services.AddFluentUIComponents();
		// FluentUI components make JS interop calls bunit has no way to know about in advance —
		// loose mode is bunit's own documented answer (same rationale as LoginTests).
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddSingleton(_service);
		Services.AddSingleton(_sessionTransition);
		Services.AddScoped<IValidator<TwoFactorSignInRequest>, TwoFactorSignInRequestValidator>();
	}

	BunitNavigationManager Navigation => Services.GetRequiredService<BunitNavigationManager>();

	[Fact]
	void The_challenge_reaches_the_form_and_is_stripped_from_the_url()
	{
		Navigation.NavigateTo("Account/LoginWith2fa?challenge=A");

		var page = Render<LoginWith2fa>();

		page.FindComponent<TwoFactorForm>().Instance.Challenge.ShouldBe("A");
		AssertLatestEntryIsTheStrippedRoute();
	}

	[Fact]
	void A_stripped_url_keeps_the_token()
	{
		Navigation.NavigateTo("Account/LoginWith2fa?challenge=A");
		var page = Render<LoginWith2fa>();

		page.Render();

		page.FindComponent<TwoFactorForm>().Instance.Challenge.ShouldBe("A");
		page.FindAll(".norse-challenge-expired").ShouldBeEmpty();
	}

	[Fact]
	async Task A_second_challenge_replaces_the_first_and_is_stripped_too()
	{
		_service.TwoFactorSignIn(Arg.Any<TwoFactorSignInRequest>(), Arg.Any<CancellationToken>())
			.Returns(Outcome<NavigationResult>.Ok(new() { NextUrl = "/" }));
		Navigation.NavigateTo("Account/LoginWith2fa?challenge=A");
		var page = Render<LoginWith2fa>();

		await page.InvokeAsync(() => Navigation.NavigateTo("Account/LoginWith2fa?challenge=B"));

		page.FindComponent<TwoFactorForm>().Instance.Challenge.ShouldBe("B");
		AssertLatestEntryIsTheStrippedRoute();
		await page.FindAll("fluent-text-input")[0].ChangeAsync("123456");
		await page.Find("form").SubmitAsync();
		await _service.Received(1).TwoFactorSignIn(Arg.Is<TwoFactorSignInRequest>(r => r.Challenge == "B"),
			Arg.Any<CancellationToken>());
	}

	// bUnit's History is stack-ordered: the first entry is the latest navigation.
	void AssertLatestEntryIsTheStrippedRoute()
	{
		var latest = Navigation.History.First();
		latest.Uri.ShouldBe("Account/LoginWith2fa");
		latest.Options.ReplaceHistoryEntry.ShouldBeTrue();
		Navigation.Uri.ShouldBe(Navigation.ToAbsoluteUri("Account/LoginWith2fa").ToString());
	}
}
