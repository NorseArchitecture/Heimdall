using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using Norse.Abstractions.Contracts;
using Norse.AuthN.Services;

namespace Norse.AuthN.Components.FluentUI.Tests;

public sealed class ConfirmEmailChangePanelTests : BunitContext
{
	readonly IAuthenticationService _service = Substitute.For<IAuthenticationService>();
	readonly ISessionTransition _sessionTransition = Substitute.For<ISessionTransition>();

	public ConfirmEmailChangePanelTests()
	{
		Services.AddFluentUIComponents();
		// FluentUI components make JS interop calls bunit has no way to know about in advance —
		// loose mode is bunit's own documented answer (same rationale as LoginTests).
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddSingleton(_service);
		Services.AddSingleton(_sessionTransition);
	}

	BunitNavigationManager Navigation => Services.GetRequiredService<BunitNavigationManager>();

	IRenderedComponent<ConfirmEmailChangePanel> RenderPanel() =>
		Render<ConfirmEmailChangePanel>(p => p
			.Add(x => x.UserId, "u")
			.Add(x => x.Email, "new@example.com")
			.Add(x => x.Code, "c"));

	[Fact]
	void A_bare_render_performs_nothing()
	{
		RenderPanel();

		_service.DidNotReceiveWithAnyArgs().ConfirmEmailChange(default!, Xunit.TestContext.Current.CancellationToken);
		_sessionTransition.DidNotReceiveWithAnyArgs().Begin(default!);
	}

	[Fact]
	void The_confirm_click_dispatches_and_begins_the_session_transition_with_no_history()
	{
		_service.ConfirmEmailChange(
				Arg.Is<ConfirmEmailChangeRequest>(r =>
					r.UserId == "u" && r.EmailInput == "new@example.com" && r.Code == "c"),
				Arg.Any<CancellationToken>())
			.Returns(Outcome<NavigationResult>.Ok(new() { NextUrl = "/Account/Login" }));
		var panel = RenderPanel();

		panel.Find("fluent-button").Click();

		_sessionTransition.Received(1).Begin(new() { NextUrl = "/Account/Login" });
		Navigation.History.ShouldBeEmpty();
	}

	[Fact]
	void A_failure_renders_the_error_and_never_transitions()
	{
		_service.ConfirmEmailChange(Arg.Any<ConfirmEmailChangeRequest>(), Arg.Any<CancellationToken>())
			.Returns(Outcome<NavigationResult>.Err(ErrorCategory.Fault, correlationId: Guid.Empty));
		var panel = RenderPanel();

		panel.Find("fluent-button").Click();

		panel.Find(".alert-danger").TextContent.ShouldContain("Error changing email.");
		panel.FindAll("fluent-button").ShouldHaveSingleItem();
		_sessionTransition.DidNotReceiveWithAnyArgs().Begin(default!);
	}

	[Fact]
	void Missing_parameters_render_the_invalid_link_state_without_a_button()
	{
		var panel = Render<ConfirmEmailChangePanel>(p => p.Add(x => x.UserId, "u").Add(x => x.Code, "c"));

		panel.Find(".norse-challenge-expired").TextContent.ShouldContain("This confirmation link is invalid.");
		panel.FindAll("fluent-button").ShouldBeEmpty();
	}
}
