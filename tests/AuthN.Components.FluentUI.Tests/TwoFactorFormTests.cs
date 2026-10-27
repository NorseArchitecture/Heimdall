using Bunit;
using Bunit.TestDoubles;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using Norse.Abstractions.Contracts;
using Norse.AuthN.Services;

namespace Norse.AuthN.Components.FluentUI.Tests;

public sealed class TwoFactorFormTests : BunitContext
{
	readonly IAuthenticationService _service = Substitute.For<IAuthenticationService>();
	readonly ISessionTransition _sessionTransition = Substitute.For<ISessionTransition>();

	public TwoFactorFormTests()
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
	void A_missing_challenge_renders_the_expired_state_without_calling_the_service()
	{
		var form = Render<TwoFactorForm>();

		form.Find(".norse-challenge-expired").TextContent.ShouldContain("expired");
		form.FindAll("form").ShouldBeEmpty();
		_service.DidNotReceiveWithAnyArgs().TwoFactorSignIn(default!, Xunit.TestContext.Current.CancellationToken);
	}

	[Fact]
	async Task Success_begins_the_session_transition_with_the_server_url_and_no_history()
	{
		_service.TwoFactorSignIn(
				Arg.Is<TwoFactorSignInRequest>(r => r.Challenge == "tok" && r.Code == "123456" && r.RememberMachine),
				Arg.Any<CancellationToken>())
			.Returns(Outcome<NavigationResult>.Ok(new() { NextUrl = "/" }));
		var form = Render<TwoFactorForm>(p => p.Add(f => f.Challenge, "tok"));

		await form.FindAll("fluent-text-input")[0].ChangeAsync("123456");
		await form.Find("fluent-checkbox").ChangeAsync(true);
		await form.Find("form").SubmitAsync();

		_sessionTransition.Received(1).Begin(new() { NextUrl = "/" });
		Navigation.History.ShouldBeEmpty();
	}

	[Theory]
	[InlineData(ErrorCategory.InvalidCredentials, "Invalid email or password.")] // an in-process outcome carrying some server text
	[InlineData(ErrorCategory.Unauthorized, null)] // the gRPC-decoded outcome: trailerless Unauthenticated, no errors
	async Task A_credential_failure_in_either_wire_shape_renders_the_local_message(ErrorCategory category,
		string? serverText)
	{
		var failed = serverText is null ?
			new Failed(new Problem { Category = category }) :
			new Failed(Problem.ModelError(category, serverText));
		_service.TwoFactorSignIn(Arg.Any<TwoFactorSignInRequest>(), Arg.Any<CancellationToken>())
			.Returns(new Outcome<NavigationResult>(failed));
		var form = Render<TwoFactorForm>(p => p.Add(f => f.Challenge, "tok"));

		await form.FindAll("fluent-text-input")[0].ChangeAsync("123456");
		await form.Find("form").SubmitAsync();

		form.Markup.ShouldContain("Invalid authenticator code.");
		form.Markup.ShouldNotContain("Invalid email or password.");
	}

	// The deadlock lock (same shape as LoginTests): a rejected code must be correctable — the
	// server-error store has to yield to the next validation pass, or the resubmit never dispatches.
	[Fact]
	async Task An_invalid_code_can_be_corrected_and_resubmitted()
	{
		_service.TwoFactorSignIn(Arg.Any<TwoFactorSignInRequest>(), Arg.Any<CancellationToken>())
			.Returns(
				new Outcome<NavigationResult>(new Failed(new Problem { Category = ErrorCategory.Unauthorized })),
				Outcome<NavigationResult>.Ok(new() { NextUrl = "/" }));
		var form = Render<TwoFactorForm>(p => p.Add(f => f.Challenge, "tok"));
		var input = form.FindAll("fluent-text-input")[0];

		await input.ChangeAsync("111111");
		await form.Find("form").SubmitAsync();
		form.Markup.ShouldContain("Invalid authenticator code.");

		await input.ChangeAsync("222222");
		await form.Find("form").SubmitAsync();

		await _service.Received(2).TwoFactorSignIn(Arg.Any<TwoFactorSignInRequest>(), Arg.Any<CancellationToken>());
		_sessionTransition.Received(1).Begin(new() { NextUrl = "/" });
	}

	[Fact]
	void The_recovery_code_link_carries_the_same_challenge() =>
		Render<TwoFactorForm>(p => p.Add(f => f.Challenge, "a b")).Find("a[href*='LoginWithRecoveryCode']")
			.GetAttribute("href").ShouldBe("Account/LoginWithRecoveryCode?challenge=a%20b");

	[Fact]
	async Task Locked_out_renders_the_server_text_in_the_summary()
	{
		_service.TwoFactorSignIn(Arg.Any<TwoFactorSignInRequest>(), Arg.Any<CancellationToken>())
			.Returns(new Outcome<NavigationResult>(new Failed(
				Problem.ModelError(ErrorCategory.LockedOut, "Your account is locked. Try again in 15 minutes."))));
		var form = Render<TwoFactorForm>(p => p.Add(f => f.Challenge, "tok"));

		await form.FindAll("fluent-text-input")[0].ChangeAsync("123456");
		await form.Find("form").SubmitAsync();

		form.Find(".norse-model-errors").TextContent.ShouldContain("Your account is locked. Try again in 15 minutes.");
		form.Markup.ShouldNotContain("Invalid authenticator code.");
		_sessionTransition.DidNotReceiveWithAnyArgs().Begin(default!);
	}

	// A present-but-expired challenge collapses onto the credential failure server-side, so the form
	// state itself must carry an exit back to Login — the sibling link rides the same dead token.
	[Fact]
	void The_form_state_links_back_to_login()
	{
		var form = Render<TwoFactorForm>(p => p.Add(f => f.Challenge, "tok"));

		form.FindAll("a[href='Account/Login']").ShouldHaveSingleItem().TextContent.ShouldContain("Start again");
		form.Find("form").ShouldNotBeNull();
		form.FindAll("fluent-button").ShouldNotBeEmpty();
	}
}
