using Bunit;
using Bunit.TestDoubles;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using Norse.Abstractions.Contracts;
using Norse.AuthN.Components.FluentUI.Manage;
using Norse.AuthN.Services;

namespace Norse.AuthN.Components.FluentUI.Tests.Manage;

public sealed class SetPasswordFormTests : InteractiveBunitContext
{
	readonly IAccountService _account = Substitute.For<IAccountService>();
	readonly ISessionTransition _sessionTransition = Substitute.For<ISessionTransition>();

	public SetPasswordFormTests()
	{
		Services.AddFluentUIComponents();
		// FluentUI components make JS interop calls bunit has no way to know about in advance —
		// loose mode is bunit's own documented answer (same rationale as LoginTests).
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services
			.AddSingleton(_account)
			.AddSingleton(_sessionTransition)
			.AddScoped<IValidator<SetPasswordRequest>, SetPasswordRequestValidator>();
	}

	BunitNavigationManager Navigation => Services.GetRequiredService<BunitNavigationManager>();

	static async Task FillAndSubmitAsync(IRenderedComponent<SetPasswordForm> form, string newPassword)
	{
		await form.Find("fluent-text-input").ChangeAsync(newPassword);
		await form.Find("form").SubmitAsync();
	}

	[Fact]
	async Task An_empty_submit_never_reaches_the_service()
	{
		var form = Render<SetPasswordForm>();

		await form.Find("form").SubmitAsync();

		await _account.DidNotReceiveWithAnyArgs().SetPassword(default!, Xunit.TestContext.Current.CancellationToken);
		_sessionTransition.DidNotReceiveWithAnyArgs().Begin(default!);
	}

	[Fact]
	async Task Success_begins_one_transition_and_no_history()
	{
		NavigationResult next = new() { NextUrl = "/Account/Manage/SetPassword?status=password-set" };
		_account.SetPassword(Arg.Any<SetPasswordRequest>(), Arg.Any<CancellationToken>())
			.Returns(Outcome<NavigationResult>.Ok(next));
		var form = Render<SetPasswordForm>();

		await FillAndSubmitAsync(form, "battery staple 2");

		await _account.Received(1).SetPassword(
			Arg.Is<SetPasswordRequest>(r => r.NewPassword == "battery staple 2"),
			Arg.Any<CancellationToken>());
		_sessionTransition.Received(1).Begin(next);
		Navigation.History.ShouldBeEmpty();
	}

	[Fact]
	async Task A_conflict_renders_in_the_summary_without_a_transition()
	{
		const string AlreadySet = "User already has a password set.";
		_account.SetPassword(Arg.Any<SetPasswordRequest>(), Arg.Any<CancellationToken>())
			.Returns(new Outcome<NavigationResult>(new Failed(Problem.ModelError(ErrorCategory.Conflict, AlreadySet))));
		var form = Render<SetPasswordForm>();

		await FillAndSubmitAsync(form, "battery staple 2");

		form.Find(".norse-model-errors").TextContent.ShouldContain(AlreadySet);
		_sessionTransition.DidNotReceiveWithAnyArgs().Begin(default!);
		Navigation.History.ShouldBeEmpty();
	}

	[Fact]
	async Task A_password_rule_failure_lands_on_the_new_password_field()
	{
		const string PasswordRule = "Passwords must have at least one digit ('0'-'9').";
		_account.SetPassword(Arg.Any<SetPasswordRequest>(), Arg.Any<CancellationToken>())
			.Returns(Outcome<NavigationResult>.Err(ErrorCategory.Validation,
				new Dictionary<string, string[]> { ["NewPassword"] = [PasswordRule] }));
		var form = Render<SetPasswordForm>();

		await FillAndSubmitAsync(form, "battery staple");

		form.Markup.ShouldContain(PasswordRule);
		_sessionTransition.DidNotReceiveWithAnyArgs().Begin(default!);
		Navigation.History.ShouldBeEmpty();
	}
}
