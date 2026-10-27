using Bunit;
using Bunit.TestDoubles;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using Norse.Abstractions.Contracts;
using Norse.AuthN.Services;

namespace Norse.AuthN.Components.FluentUI.Tests;

public sealed class ResetPasswordFormTests : BunitContext
{
	readonly IAuthenticationService _service = Substitute.For<IAuthenticationService>();

	public ResetPasswordFormTests()
	{
		Services.AddFluentUIComponents();
		// FluentUI components make JS interop calls bunit has no way to know about in advance —
		// loose mode is bunit's own documented answer (same rationale as LoginTests).
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddSingleton(_service);
		Services.AddScoped<IValidator<ResetPasswordRequest>, ResetPasswordRequestValidator>();
	}

	BunitNavigationManager Navigation => Services.GetRequiredService<BunitNavigationManager>();

	[Fact]
	async Task An_empty_submit_never_reaches_the_service()
	{
		var form = Render<ResetPasswordForm>(p => p.Add(f => f.Code, "code-1"));

		await form.Find("form").SubmitAsync();

		await _service.DidNotReceiveWithAnyArgs().ResetPassword(default!, Xunit.TestContext.Current.CancellationToken);
	}

	[Fact]
	async Task Success_navigates_softly_to_the_confirmation_url()
	{
		_service.ResetPassword(
				Arg.Is<ResetPasswordRequest>(r =>
					r.EmailInput == "user@example.com" && r.Code == "code-1" && r.Password == "correct horse 1"),
				Arg.Any<CancellationToken>())
			.Returns(Outcome<NavigationResult>.Ok(new() { NextUrl = "/Account/ResetPasswordConfirmation" }));
		var form = Render<ResetPasswordForm>(p => p.Add(f => f.Code, "code-1"));

		await FillAndSubmitAsync(form);

		var entry = Navigation.History.ShouldHaveSingleItem();
		entry.Uri.ShouldBe("/Account/ResetPasswordConfirmation");
		entry.Options.ForceLoad.ShouldBeFalse();
	}

	[Fact]
	async Task A_password_rule_failure_lands_on_the_password_field()
	{
		const string PasswordRule = "Passwords must have at least one digit ('0'-'9').";
		_service.ResetPassword(Arg.Any<ResetPasswordRequest>(), Arg.Any<CancellationToken>())
			.Returns(Outcome<NavigationResult>.Err(ErrorCategory.Validation,
				new Dictionary<string, string[]> { ["Password"] = [PasswordRule] }));
		var form = Render<ResetPasswordForm>(p => p.Add(f => f.Code, "code-1"));

		await FillAndSubmitAsync(form);

		form.Markup.ShouldContain(PasswordRule);
		Navigation.History.ShouldBeEmpty();
	}

	[Theory]
	[InlineData(ErrorCategory.InvalidCredentials, "Invalid token.")] // an in-process outcome carrying the handler's text
	[InlineData(ErrorCategory.Unauthorized, null)] // the gRPC-decoded outcome: trailerless Unauthenticated, no errors
	async Task A_credential_failure_in_either_wire_shape_renders_the_local_message(ErrorCategory category,
		string? serverText)
	{
		var failed = serverText is null ?
			new Failed(new Problem { Category = category }) :
			new Failed(Problem.ModelError(category, serverText));
		_service.ResetPassword(Arg.Any<ResetPasswordRequest>(), Arg.Any<CancellationToken>())
			.Returns(new Outcome<NavigationResult>(failed));
		var form = Render<ResetPasswordForm>(p => p.Add(f => f.Code, "code-1"));

		await FillAndSubmitAsync(form);

		form.Markup.ShouldContain("The reset link is invalid or has expired.");
		form.Markup.ShouldNotContain("Invalid token.");
		Navigation.History.ShouldBeEmpty();
	}

	static async Task FillAndSubmitAsync(IRenderedComponent<ResetPasswordForm> form)
	{
		var inputs = form.FindAll("fluent-text-input");
		await inputs[0].ChangeAsync("user@example.com");
		await inputs[1].ChangeAsync("correct horse 1");
		await form.Find("form").SubmitAsync();
	}

	[Fact]
	async Task An_empty_submit_renders_the_email_required_message()
	{
		var form = Render<ResetPasswordForm>(p => p.Add(f => f.Code, "code-1"));

		await form.Find("form").SubmitAsync();

		form.Markup.ShouldContain("Enter your email address.");
		await _service.DidNotReceiveWithAnyArgs().ResetPassword(default!, Xunit.TestContext.Current.CancellationToken);
	}

	[Fact]
	async Task A_malformed_email_renders_the_format_message()
	{
		var form = Render<ResetPasswordForm>(p => p.Add(f => f.Code, "code-1"));

		await form.FindAll("fluent-text-input")[0].ChangeAsync("not-an-email");
		await form.Find("form").SubmitAsync();

		form.Markup.ShouldContain("Enter a valid email address (local@domain.tld).");
		await _service.DidNotReceiveWithAnyArgs().ResetPassword(default!, Xunit.TestContext.Current.CancellationToken);
	}
}
