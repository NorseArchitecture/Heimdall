using Bunit;
using Bunit.TestDoubles;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using Norse.Abstractions.Contracts;
using Norse.AuthN.Components.FluentUI.Manage;
using Norse.AuthN.Services;

namespace Norse.AuthN.Components.FluentUI.Tests.Manage;

public sealed class ChangePasswordFormTests : InteractiveBunitContext
{
	readonly IAccountService _account = Substitute.For<IAccountService>();
	readonly ISessionTransition _sessionTransition = Substitute.For<ISessionTransition>();

	public ChangePasswordFormTests()
	{
		Services.AddFluentUIComponents();
		// FluentUI components make JS interop calls bunit has no way to know about in advance —
		// loose mode is bunit's own documented answer (same rationale as LoginTests).
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services
			.AddSingleton(_account)
			.AddSingleton(_sessionTransition)
			.AddScoped<IValidator<ChangePasswordRequest>, ChangePasswordRequestValidator>();
	}

	BunitNavigationManager Navigation => Services.GetRequiredService<BunitNavigationManager>();

	static async Task FillAndSubmitAsync(IRenderedComponent<ChangePasswordForm> form, string oldPassword,
		string newPassword)
	{
		var inputs = form.FindAll("fluent-text-input");
		await inputs[0].ChangeAsync(oldPassword);
		await inputs[1].ChangeAsync(newPassword);
		await form.Find("form").SubmitAsync();
	}

	[Fact]
	async Task An_empty_submit_never_reaches_the_service()
	{
		var form = Render<ChangePasswordForm>();

		await form.Find("form").SubmitAsync();

		await _account.DidNotReceiveWithAnyArgs().ChangePassword(default!, Xunit.TestContext.Current.CancellationToken);
		_sessionTransition.DidNotReceiveWithAnyArgs().Begin(default!);
	}

	[Fact]
	async Task A_new_password_equal_to_the_old_one_never_reaches_the_service()
	{
		var form = Render<ChangePasswordForm>();

		await FillAndSubmitAsync(form, "correct horse 1", "correct horse 1");

		form.Markup.ShouldContain("Your new password must differ from your current password.");
		await _account.DidNotReceiveWithAnyArgs().ChangePassword(default!, Xunit.TestContext.Current.CancellationToken);
	}

	[Fact]
	async Task Success_begins_one_transition_and_no_history()
	{
		NavigationResult next = new() { NextUrl = "/Account/Manage/ChangePassword?status=password-changed" };
		_account.ChangePassword(Arg.Any<ChangePasswordRequest>(), Arg.Any<CancellationToken>())
			.Returns(Outcome<NavigationResult>.Ok(next));
		var form = Render<ChangePasswordForm>();

		await FillAndSubmitAsync(form, "correct horse 1", "battery staple 2");

		await _account.Received(1).ChangePassword(
			Arg.Is<ChangePasswordRequest>(r => r.OldPassword == "correct horse 1" && r.NewPassword == "battery staple 2"),
			Arg.Any<CancellationToken>());
		_sessionTransition.Received(1).Begin(next);
		Navigation.History.ShouldBeEmpty();
	}

	[Fact]
	async Task A_wrong_current_password_lands_on_the_old_password_field()
	{
		const string Incorrect = "Incorrect password.";
		_account.ChangePassword(Arg.Any<ChangePasswordRequest>(), Arg.Any<CancellationToken>())
			.Returns(Outcome<NavigationResult>.Err(ErrorCategory.InvalidCredentials,
				new Dictionary<string, string[]> { ["OldPassword"] = [Incorrect] }));
		var form = Render<ChangePasswordForm>();

		await FillAndSubmitAsync(form, "wrong horse 1", "battery staple 2");

		form.Markup.ShouldContain(Incorrect);
		_sessionTransition.DidNotReceiveWithAnyArgs().Begin(default!);
		Navigation.History.ShouldBeEmpty();
	}

	// The deadlock lock (spec §10, same shape as LoginTests): a field-keyed rejection must be correctable —
	// the server-error store has to yield to the next validation pass, or the resubmit never dispatches.
	[Fact]
	async Task A_rejected_current_password_can_be_corrected_and_resubmitted()
	{
		const string Incorrect = "Incorrect password.";
		NavigationResult next = new() { NextUrl = "/Account/Manage/ChangePassword?status=password-changed" };
		_account.ChangePassword(Arg.Any<ChangePasswordRequest>(), Arg.Any<CancellationToken>())
			.Returns(
				Outcome<NavigationResult>.Err(ErrorCategory.InvalidCredentials,
					new Dictionary<string, string[]> { ["OldPassword"] = [Incorrect] }),
				Outcome<NavigationResult>.Ok(next));
		var form = Render<ChangePasswordForm>();

		await FillAndSubmitAsync(form, "wrong horse 1", "battery staple 2");
		form.Markup.ShouldContain(Incorrect);

		await form.FindAll("fluent-text-input")[0].ChangeAsync("correct horse 1");
		await form.Find("form").SubmitAsync();

		await _account.Received(2).ChangePassword(Arg.Any<ChangePasswordRequest>(), Arg.Any<CancellationToken>());
		form.Markup.ShouldNotContain(Incorrect);
		_sessionTransition.Received(1).Begin(next);
	}

	[Fact]
	async Task A_password_rule_failure_lands_on_the_new_password_field()
	{
		const string PasswordRule = "Passwords must have at least one digit ('0'-'9').";
		_account.ChangePassword(Arg.Any<ChangePasswordRequest>(), Arg.Any<CancellationToken>())
			.Returns(Outcome<NavigationResult>.Err(ErrorCategory.Validation,
				new Dictionary<string, string[]> { ["NewPassword"] = [PasswordRule] }));
		var form = Render<ChangePasswordForm>();

		await FillAndSubmitAsync(form, "correct horse 1", "battery staple");

		form.Markup.ShouldContain(PasswordRule);
		_sessionTransition.DidNotReceiveWithAnyArgs().Begin(default!);
		Navigation.History.ShouldBeEmpty();
	}
}
