using Bunit;
using Bunit.TestDoubles;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using Norse.Abstractions.Contracts;
using Norse.AuthN.Services;

namespace Norse.AuthN.Components.FluentUI.Tests;

public sealed class RecoveryCodeFormTests : InteractiveBunitContext
{
	readonly IAuthenticationService _service = Substitute.For<IAuthenticationService>();
	readonly ISessionTransition _sessionTransition = Substitute.For<ISessionTransition>();

	public RecoveryCodeFormTests()
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
	void A_missing_challenge_renders_the_expired_state_without_calling_the_service()
	{
		var form = Render<RecoveryCodeForm>();

		form.Find(".norse-challenge-expired").TextContent.ShouldContain("expired");
		form.FindAll("form").ShouldBeEmpty();
		_service.DidNotReceiveWithAnyArgs().RecoveryCodeSignIn(default!, Xunit.TestContext.Current.CancellationToken);
	}

	[Fact]
	void The_form_has_a_single_recovery_code_input_and_no_remember_machine_checkbox()
	{
		var form = Render<RecoveryCodeForm>(p => p.Add(f => f.Challenge, "tok"));

		var input = form.FindAll("fluent-text-input").ShouldHaveSingleItem();
		form.Find($"label[for='{input.Id}']").TextContent.ShouldContain("Recovery code");
		form.FindAll("fluent-checkbox").ShouldBeEmpty();
	}

	[Fact]
	async Task Success_begins_the_session_transition_with_the_server_url_and_no_history()
	{
		_service.RecoveryCodeSignIn(
				Arg.Is<RecoveryCodeSignInRequest>(r => r.Challenge == "tok" && r.RecoveryCode == "ABCDE-12345"),
				Arg.Any<CancellationToken>())
			.Returns(Outcome<NavigationResult>.Ok(new() { NextUrl = "/" }));
		var form = Render<RecoveryCodeForm>(p => p.Add(f => f.Challenge, "tok"));

		await form.FindAll("fluent-text-input")[0].ChangeAsync("ABCDE-12345");
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
		_service.RecoveryCodeSignIn(Arg.Any<RecoveryCodeSignInRequest>(), Arg.Any<CancellationToken>())
			.Returns(new Outcome<NavigationResult>(failed));
		var form = Render<RecoveryCodeForm>(p => p.Add(f => f.Challenge, "tok"));

		await form.FindAll("fluent-text-input")[0].ChangeAsync("ABCDE-12345");
		await form.Find("form").SubmitAsync();

		form.Markup.ShouldContain("Invalid recovery code entered.");
		form.Markup.ShouldNotContain("Invalid email or password.");
	}

	// The deadlock lock (same shape as LoginTests): a rejected code must be correctable — the
	// server-error store has to yield to the next validation pass, or the resubmit never dispatches.
	[Fact]
	async Task An_invalid_code_can_be_corrected_and_resubmitted()
	{
		_service.RecoveryCodeSignIn(Arg.Any<RecoveryCodeSignInRequest>(), Arg.Any<CancellationToken>())
			.Returns(
				new Outcome<NavigationResult>(new Failed(new Problem { Category = ErrorCategory.Unauthorized })),
				Outcome<NavigationResult>.Ok(new() { NextUrl = "/" }));
		var form = Render<RecoveryCodeForm>(p => p.Add(f => f.Challenge, "tok"));
		var input = form.FindAll("fluent-text-input")[0];

		await input.ChangeAsync("AAAAA-11111");
		await form.Find("form").SubmitAsync();
		form.Markup.ShouldContain("Invalid recovery code entered.");

		await input.ChangeAsync("BBBBB-22222");
		await form.Find("form").SubmitAsync();

		await _service.Received(2).RecoveryCodeSignIn(Arg.Any<RecoveryCodeSignInRequest>(), Arg.Any<CancellationToken>());
		_sessionTransition.Received(1).Begin(new() { NextUrl = "/" });
	}

	[Fact]
	void The_authenticator_link_carries_the_same_challenge() =>
		Render<RecoveryCodeForm>(p => p.Add(f => f.Challenge, "a b")).Find("a[href*='LoginWith2fa']")
			.GetAttribute("href").ShouldBe("Account/LoginWith2fa?challenge=a%20b");

	[Fact]
	async Task Locked_out_renders_the_server_text_in_the_summary()
	{
		_service.RecoveryCodeSignIn(Arg.Any<RecoveryCodeSignInRequest>(), Arg.Any<CancellationToken>())
			.Returns(new Outcome<NavigationResult>(new Failed(
				Problem.ModelError(ErrorCategory.LockedOut, "Your account is locked. Try again in 15 minutes."))));
		var form = Render<RecoveryCodeForm>(p => p.Add(f => f.Challenge, "tok"));

		await form.FindAll("fluent-text-input")[0].ChangeAsync("ABCDE-12345");
		await form.Find("form").SubmitAsync();

		form.Find(".norse-model-errors").TextContent.ShouldContain("Your account is locked. Try again in 15 minutes.");
		form.Markup.ShouldNotContain("Invalid recovery code entered.");
		_sessionTransition.DidNotReceiveWithAnyArgs().Begin(default!);
	}

	// A present-but-expired challenge collapses onto the credential failure server-side, so the form
	// state itself must carry an exit back to Login — the sibling link rides the same dead token.
	[Fact]
	void The_form_state_links_back_to_login()
	{
		var form = Render<RecoveryCodeForm>(p => p.Add(f => f.Challenge, "tok"));

		form.FindAll("a[href='Account/Login']").ShouldHaveSingleItem().TextContent.ShouldContain("Start again");
		form.Find("form").ShouldNotBeNull();
		form.FindAll("fluent-button").ShouldNotBeEmpty();
	}
}
