using Bunit;
using Bunit.TestDoubles;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using Norse.Abstractions.Contracts;
using Norse.AuthN.Services;

namespace Norse.AuthN.Components.FluentUI.Tests;

public sealed class LoginWithRecoveryCodeTests : InteractiveBunitContext
{
	readonly IAuthenticationService _service = Substitute.For<IAuthenticationService>();
	readonly ISessionTransition _sessionTransition = Substitute.For<ISessionTransition>();

	public LoginWithRecoveryCodeTests()
	{
		Services.AddFluentUIComponents();
		// FluentUI components make JS interop calls bunit has no way to know about in advance —
		// loose mode is bunit's own documented answer (same rationale as LoginTests).
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddSingleton(_service);
		Services.AddSingleton(_sessionTransition);
		Services.AddScoped<IValidator<RecoveryCodeSignInRequest>, RecoveryCodeSignInRequestValidator>();
	}

	BunitNavigationManager Navigation => Services.GetRequiredService<BunitNavigationManager>();

	[Fact]
	void The_challenge_reaches_the_form_and_is_stripped_from_the_url()
	{
		Navigation.NavigateTo("Account/LoginWithRecoveryCode?challenge=A");

		var page = Render<LoginWithRecoveryCode>();

		page.FindComponent<RecoveryCodeForm>().Instance.Challenge.ShouldBe("A");
		AssertLatestEntryIsTheStrippedRoute();
	}

	[Fact]
	void A_stripped_url_keeps_the_token()
	{
		Navigation.NavigateTo("Account/LoginWithRecoveryCode?challenge=A");
		var page = Render<LoginWithRecoveryCode>();

		page.Render();

		page.FindComponent<RecoveryCodeForm>().Instance.Challenge.ShouldBe("A");
		page.FindAll(".norse-challenge-expired").ShouldBeEmpty();
	}

	[Fact]
	async Task A_second_challenge_replaces_the_first_and_is_stripped_too()
	{
		_service.RecoveryCodeSignIn(Arg.Any<RecoveryCodeSignInRequest>(), Arg.Any<CancellationToken>())
			.Returns(Outcome<NavigationResult>.Ok(new() { NextUrl = "/" }));
		Navigation.NavigateTo("Account/LoginWithRecoveryCode?challenge=A");
		var page = Render<LoginWithRecoveryCode>();

		await page.InvokeAsync(() => Navigation.NavigateTo("Account/LoginWithRecoveryCode?challenge=B"));

		page.FindComponent<RecoveryCodeForm>().Instance.Challenge.ShouldBe("B");
		AssertLatestEntryIsTheStrippedRoute();
		await page.FindAll("fluent-text-input")[0].ChangeAsync("ABCDE-12345");
		await page.Find("form").SubmitAsync();
		await _service.Received(1).RecoveryCodeSignIn(Arg.Is<RecoveryCodeSignInRequest>(r => r.Challenge == "B"),
			Arg.Any<CancellationToken>());
	}

	// bUnit's History is stack-ordered: the first entry is the latest navigation.
	void AssertLatestEntryIsTheStrippedRoute()
	{
		var latest = Navigation.History.First();
		latest.Uri.ShouldBe("Account/LoginWithRecoveryCode");
		latest.Options.ReplaceHistoryEntry.ShouldBeTrue();
		Navigation.Uri.ShouldBe(Navigation.ToAbsoluteUri("Account/LoginWithRecoveryCode").ToString());
	}
}
