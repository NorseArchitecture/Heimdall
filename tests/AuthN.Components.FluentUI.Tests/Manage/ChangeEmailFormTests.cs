using Bunit;
using Bunit.TestDoubles;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using Norse.Abstractions.Contracts;
using Norse.AuthN.Components.FluentUI.Manage;
using Norse.AuthN.Services;

namespace Norse.AuthN.Components.FluentUI.Tests.Manage;

public sealed class ChangeEmailFormTests : InteractiveBunitContext
{
	readonly IAccountService _account = Substitute.For<IAccountService>();
	readonly IAuthenticationService _authentication = Substitute.For<IAuthenticationService>();

	public ChangeEmailFormTests()
	{
		Services.AddFluentUIComponents();
		// FluentUI components make JS interop calls bunit has no way to know about in advance —
		// loose mode is bunit's own documented answer (same rationale as LoginTests).
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services
			.AddSingleton(_account)
			.AddSingleton(_authentication)
			.AddScoped<IValidator<ChangeEmailRequest>, ChangeEmailRequestValidator>();
	}

	BunitNavigationManager Navigation => Services.GetRequiredService<BunitNavigationManager>();

	IRenderedComponent<ChangeEmailForm> RenderForm(bool emailConfirmed = true) =>
		Render<ChangeEmailForm>(p => p.Add(f => f.Email, "user@example.com").Add(f => f.EmailConfirmed, emailConfirmed));

	static async Task ChangeEmailAndSubmitAsync(IRenderedComponent<ChangeEmailForm> form, string newEmail)
	{
		await form.FindAll("fluent-text-input")[1].ChangeAsync(newEmail);
		await form.Find("form").SubmitAsync();
	}

	[Fact]
	async Task An_empty_submit_never_reaches_the_service()
	{
		var form = RenderForm();

		await form.Find("form").SubmitAsync();

		form.Markup.ShouldContain("Enter your new email address.");
		await _account.DidNotReceiveWithAnyArgs().ChangeEmail(default!, Xunit.TestContext.Current.CancellationToken);
	}

	[Fact]
	async Task A_malformed_email_renders_the_format_message()
	{
		var form = RenderForm();

		await ChangeEmailAndSubmitAsync(form, "not-an-email");

		form.Markup.ShouldContain("Enter a valid email address (local@domain.tld).");
		await _account.DidNotReceiveWithAnyArgs().ChangeEmail(default!, Xunit.TestContext.Current.CancellationToken);
	}

	[Fact]
	async Task Success_renders_the_sent_message_in_place()
	{
		_account.ChangeEmail(Arg.Any<ChangeEmailRequest>(), Arg.Any<CancellationToken>())
			.Returns(Outcome<BoolResponse>.Ok(new() { Value = true }));
		var form = RenderForm();

		await ChangeEmailAndSubmitAsync(form, "new@example.com");

		form.Markup.ShouldContain("Confirmation link to change email sent.");
		await _account.Received(1).ChangeEmail(
			Arg.Is<ChangeEmailRequest>(r => r.NewEmailInput == "new@example.com"),
			Arg.Any<CancellationToken>());
		Navigation.History.ShouldBeEmpty();
	}

	[Fact]
	async Task An_unchanged_email_lands_on_the_new_email_field()
	{
		const string Unchanged = "Your email is unchanged.";
		_account.ChangeEmail(Arg.Any<ChangeEmailRequest>(), Arg.Any<CancellationToken>())
			.Returns(Outcome<BoolResponse>.Err(ErrorCategory.Validation,
				new Dictionary<string, string[]> { ["NewEmail"] = [Unchanged] }));
		var form = RenderForm();

		await ChangeEmailAndSubmitAsync(form, "user@example.com");

		form.Markup.ShouldContain(Unchanged);
		await _account.Received(1).ChangeEmail(Arg.Any<ChangeEmailRequest>(), Arg.Any<CancellationToken>());
		Navigation.History.ShouldBeEmpty();
	}

	// The deadlock lock (spec §10, same shape as LoginTests): a field-keyed rejection must be correctable —
	// the server-error store has to yield to the next validation pass, or the resubmit never dispatches.
	[Fact]
	async Task A_rejected_new_email_can_be_corrected_and_resubmitted()
	{
		const string Unchanged = "Your email is unchanged.";
		_account.ChangeEmail(Arg.Any<ChangeEmailRequest>(), Arg.Any<CancellationToken>())
			.Returns(
				Outcome<BoolResponse>.Err(ErrorCategory.Validation,
					new Dictionary<string, string[]> { ["NewEmail"] = [Unchanged] }),
				Outcome<BoolResponse>.Ok(new() { Value = true }));
		var form = RenderForm();

		await ChangeEmailAndSubmitAsync(form, "user@example.com");
		form.Markup.ShouldContain(Unchanged);

		await ChangeEmailAndSubmitAsync(form, "new@example.com");

		await _account.Received(2).ChangeEmail(Arg.Any<ChangeEmailRequest>(), Arg.Any<CancellationToken>());
		form.Markup.ShouldNotContain(Unchanged);
		form.Markup.ShouldContain("Confirmation link to change email sent.");
	}

	[Fact]
	void The_verification_button_renders_only_when_unconfirmed()
	{
		RenderForm(emailConfirmed: true).Markup.ShouldNotContain("Send verification email");
		RenderForm(emailConfirmed: false).Markup.ShouldContain("Send verification email");
	}

	[Fact]
	async Task The_verification_button_calls_resend_with_the_current_email()
	{
		_authentication.ResendEmailConfirmation(Arg.Any<ResendEmailConfirmationRequest>(), Arg.Any<CancellationToken>())
			.Returns(Outcome<BoolResponse>.Ok(new() { Value = true }));
		var form = RenderForm(emailConfirmed: false);

		await form.InvokeAsync(() => form.FindAll("fluent-button").Single(b => b.TextContent.Contains("Send verification email")).Click());

		await _authentication.Received(1).ResendEmailConfirmation(
			Arg.Is<ResendEmailConfirmationRequest>(r => r.EmailInput == "user@example.com"),
			Arg.Any<CancellationToken>());
		form.Markup.ShouldContain("Verification email sent. Please check your email.");
		Navigation.History.ShouldBeEmpty();
	}
}
