using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Norse.Abstractions.Contracts;
using Norse.AuthN.Services;

namespace Norse.AuthN.Components.Tests;

public sealed class LogoutTests : BunitContext
{
	readonly IAuthenticationService _service = Substitute.For<IAuthenticationService>();
	readonly ISessionTransition _sessionTransition = Substitute.For<ISessionTransition>();

	public LogoutTests()
	{
		Services.AddSingleton(_service);
		Services.AddSingleton(_sessionTransition);
	}

	[Fact]
	void A_bare_render_performs_no_sign_out()
	{
		Render<Logout>();

		_service.DidNotReceiveWithAnyArgs().Logout(Xunit.TestContext.Current.CancellationToken);
		_sessionTransition.DidNotReceiveWithAnyArgs().Begin(null!);
	}

	[Fact]
	void The_confirm_click_dispatches_and_begins_the_session_transition()
	{
		_service.Logout(Arg.Any<CancellationToken>())
			.Returns(Outcome<NavigationResult>.Ok(new NavigationResult { NextUrl = "/" }));
		var page = Render<Logout>();

		page.Find("button").Click();

		_sessionTransition.Received(1).Begin(new() { NextUrl = "/" });
	}

	[Fact]
	void A_deferred_completion_url_rides_the_same_transition()
	{
		_service.Logout(Arg.Any<CancellationToken>())
			.Returns(Outcome<NavigationResult>.Ok(new NavigationResult { NextUrl = "/_auth/complete?key=abc&returnUrl=%2F" }));
		var page = Render<Logout>();

		page.Find("button").Click();

		_sessionTransition.Received(1).Begin(new() { NextUrl = "/_auth/complete?key=abc&returnUrl=%2F" });
	}

	[Fact]
	void A_failed_sign_out_renders_the_problem_and_never_transitions()
	{
		_service.Logout(Arg.Any<CancellationToken>())
			.Returns(Outcome<NavigationResult>.Err(ErrorCategory.Fault, correlationId: Guid.Empty));
		var page = Render<Logout>();

		page.Find("button").Click();

		page.Find(".alert-danger").TextContent.ShouldContain("still signed in");
		_sessionTransition.DidNotReceiveWithAnyArgs().Begin(null!);
	}
}
