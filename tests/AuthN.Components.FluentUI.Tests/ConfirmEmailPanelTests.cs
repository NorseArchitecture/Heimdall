using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using Norse.Abstractions.Contracts;
using Norse.AuthN.Services;

namespace Norse.AuthN.Components.FluentUI.Tests;

public sealed class ConfirmEmailPanelTests : BunitContext
{
	readonly IAuthenticationService _service = Substitute.For<IAuthenticationService>();

	public ConfirmEmailPanelTests()
	{
		Services.AddFluentUIComponents();
		// FluentUI components make JS interop calls bunit has no way to know about in advance —
		// loose mode is bunit's own documented answer (same rationale as LoginTests).
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddSingleton(_service);
	}

	IRenderedComponent<ConfirmEmailPanel> RenderPanel() =>
		Render<ConfirmEmailPanel>(p => p.Add(x => x.UserId, "u").Add(x => x.Code, "c"));

	[Fact]
	void A_bare_render_confirms_nothing()
	{
		var panel = RenderPanel();

		panel.Markup.ShouldContain("Confirm your email address");
		_service.DidNotReceiveWithAnyArgs().ConfirmEmail(default!, Xunit.TestContext.Current.CancellationToken);
	}

	[Fact]
	void The_confirm_click_dispatches_and_renders_the_thanks()
	{
		_service.ConfirmEmail(Arg.Is<ConfirmEmailRequest>(r => r.UserId == "u" && r.Code == "c"), Arg.Any<CancellationToken>())
			.Returns(Outcome<BoolResponse>.Ok(new() { Value = true }));
		var panel = RenderPanel();

		panel.Find("fluent-button").Click();

		panel.Find("[role=alert]").TextContent.ShouldContain("Thank you for confirming");
		panel.Find("a[href='Account/Login']").ShouldNotBeNull();
		panel.FindAll("fluent-button").ShouldBeEmpty();
	}

	[Fact]
	void A_failure_renders_the_error_and_keeps_the_button()
	{
		_service.ConfirmEmail(Arg.Any<ConfirmEmailRequest>(), Arg.Any<CancellationToken>())
			.Returns(Outcome<BoolResponse>.Err(ErrorCategory.Fault, correlationId: Guid.Empty));
		var panel = RenderPanel();

		panel.Find("fluent-button").Click();

		panel.Find(".alert-danger").TextContent.ShouldContain("Error confirming your email.");
		panel.Markup.ShouldNotContain("We couldn't find that account.");
		panel.FindAll("fluent-button").ShouldHaveSingleItem();
	}

	[Fact]
	void A_not_found_failure_names_the_missing_account()
	{
		_service.ConfirmEmail(Arg.Any<ConfirmEmailRequest>(), Arg.Any<CancellationToken>())
			.Returns(Outcome<BoolResponse>.Err(ErrorCategory.NotFound));
		var panel = RenderPanel();

		panel.Find("fluent-button").Click();

		var alert = panel.Find(".alert-danger").TextContent;
		alert.ShouldContain("Error confirming your email.");
		alert.ShouldContain("We couldn't find that account.");
	}

	[Theory]
	[InlineData(null, "c")]
	[InlineData("u", null)]
	[InlineData("", "")]
	void Missing_parameters_render_the_invalid_link_state_without_a_button(string? userId, string? code)
	{
		var panel = Render<ConfirmEmailPanel>(p => p.Add(x => x.UserId, userId).Add(x => x.Code, code));

		panel.Find(".norse-challenge-expired").TextContent.ShouldContain("This confirmation link is invalid.");
		panel.FindAll("fluent-button").ShouldBeEmpty();
		_service.DidNotReceiveWithAnyArgs().ConfirmEmail(default!, Xunit.TestContext.Current.CancellationToken);
	}
}
