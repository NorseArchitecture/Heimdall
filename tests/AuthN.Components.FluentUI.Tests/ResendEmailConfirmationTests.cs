using Bunit;
using Bunit.TestDoubles;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using Norse.Abstractions.Contracts;
using Norse.AuthN.Services;

namespace Norse.AuthN.Components.FluentUI.Tests;

public sealed class ResendEmailConfirmationTests : BunitContext
{
	readonly IAuthenticationService _service = Substitute.For<IAuthenticationService>();

	public ResendEmailConfirmationTests()
	{
		Services.AddFluentUIComponents();
		// FluentUI components make JS interop calls bunit has no way to know about in advance —
		// loose mode is bunit's own documented answer (same rationale as LoginTests).
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddSingleton(_service);
		Services.AddScoped<IValidator<ResendEmailConfirmationRequest>, ResendEmailConfirmationRequestValidator>();
	}

	BunitNavigationManager Navigation => Services.GetRequiredService<BunitNavigationManager>();

	[Fact]
	async Task An_empty_submit_never_reaches_the_service()
	{
		var page = Render<ResendEmailConfirmation>();

		await page.Find("form").SubmitAsync();

		await _service.DidNotReceiveWithAnyArgs().ResendEmailConfirmation(default!, Xunit.TestContext.Current.CancellationToken);
	}

	[Fact]
	async Task Success_renders_the_sent_message_in_place()
	{
		_service.ResendEmailConfirmation(Arg.Any<ResendEmailConfirmationRequest>(), Arg.Any<CancellationToken>())
			.Returns(Outcome<BoolResponse>.Ok(new() { Value = true }));
		var page = Render<ResendEmailConfirmation>();

		await page.FindAll("fluent-text-input")[0].ChangeAsync("user@example.com");
		await page.Find("form").SubmitAsync();

		page.Find("[role=alert]").TextContent.ShouldContain("Verification email sent");
		page.FindAll("form").ShouldBeEmpty();
		Navigation.History.ShouldBeEmpty();
	}

	[Fact]
	async Task An_empty_submit_renders_the_email_required_message()
	{
		var page = Render<ResendEmailConfirmation>();

		await page.Find("form").SubmitAsync();

		page.Markup.ShouldContain("Enter your email address.");
		await _service.DidNotReceiveWithAnyArgs().ResendEmailConfirmation(default!, Xunit.TestContext.Current.CancellationToken);
	}

	[Fact]
	async Task A_malformed_email_renders_the_format_message()
	{
		var page = Render<ResendEmailConfirmation>();

		await page.FindAll("fluent-text-input")[0].ChangeAsync("not-an-email");
		await page.Find("form").SubmitAsync();

		page.Markup.ShouldContain("Enter a valid email address (local@domain.tld).");
		await _service.DidNotReceiveWithAnyArgs().ResendEmailConfirmation(default!, Xunit.TestContext.Current.CancellationToken);
	}
}
