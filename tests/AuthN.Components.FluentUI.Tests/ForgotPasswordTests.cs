using Bunit;
using Bunit.TestDoubles;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using Norse.Abstractions.Contracts;
using Norse.AuthN.Services;

namespace Norse.AuthN.Components.FluentUI.Tests;

public sealed class ForgotPasswordTests : BunitContext
{
	readonly IAuthenticationService _service = Substitute.For<IAuthenticationService>();

	public ForgotPasswordTests()
	{
		Services.AddFluentUIComponents();
		// FluentUI components make JS interop calls bunit has no way to know about in advance —
		// loose mode is bunit's own documented answer (same rationale as LoginTests).
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddSingleton(_service);
		Services.AddScoped<IValidator<ForgotPasswordRequest>, ForgotPasswordRequestValidator>();
	}

	BunitNavigationManager Navigation => Services.GetRequiredService<BunitNavigationManager>();

	[Fact]
	async Task An_empty_submit_never_reaches_the_service()
	{
		var page = Render<ForgotPassword>();

		await page.Find("form").SubmitAsync();

		await _service.DidNotReceiveWithAnyArgs().ForgotPassword(default!, Xunit.TestContext.Current.CancellationToken);
	}

	[Fact]
	async Task Success_navigates_softly_to_the_server_authored_url()
	{
		_service.ForgotPassword(Arg.Any<ForgotPasswordRequest>(), Arg.Any<CancellationToken>())
			.Returns(Outcome<NavigationResult>.Ok(new() { NextUrl = "/Account/ForgotPasswordConfirmation" }));
		var page = Render<ForgotPassword>();

		await page.FindAll("fluent-text-input")[0].ChangeAsync("user@example.com");
		await page.Find("form").SubmitAsync();

		var entry = Navigation.History.ShouldHaveSingleItem();
		entry.Uri.ShouldBe("/Account/ForgotPasswordConfirmation");
		entry.Options.ForceLoad.ShouldBeFalse();
	}

	[Fact]
	async Task A_fault_renders_in_the_model_summary_and_does_not_navigate()
	{
		_service.ForgotPassword(Arg.Any<ForgotPasswordRequest>(), Arg.Any<CancellationToken>())
			.Returns(Outcome<NavigationResult>.Err(ErrorCategory.Fault, correlationId: Guid.Empty));
		var page = Render<ForgotPassword>();

		await page.FindAll("fluent-text-input")[0].ChangeAsync("user@example.com");
		await page.Find("form").SubmitAsync();

		page.Find(".norse-model-errors").ShouldNotBeNull();
		Navigation.History.ShouldBeEmpty();
	}

	[Fact]
	async Task An_empty_submit_renders_the_email_required_message()
	{
		var page = Render<ForgotPassword>();

		await page.Find("form").SubmitAsync();

		page.Markup.ShouldContain("Enter your email address.");
		await _service.DidNotReceiveWithAnyArgs().ForgotPassword(default!, Xunit.TestContext.Current.CancellationToken);
	}

	[Fact]
	async Task A_malformed_email_renders_the_format_message()
	{
		var page = Render<ForgotPassword>();

		await page.FindAll("fluent-text-input")[0].ChangeAsync("not-an-email");
		await page.Find("form").SubmitAsync();

		page.Markup.ShouldContain("Enter a valid email address (local@domain.tld).");
		await _service.DidNotReceiveWithAnyArgs().ForgotPassword(default!, Xunit.TestContext.Current.CancellationToken);
	}
}
