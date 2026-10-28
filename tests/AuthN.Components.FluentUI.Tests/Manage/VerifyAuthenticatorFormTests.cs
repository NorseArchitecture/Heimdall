using Bunit;
using Bunit.TestDoubles;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using Norse.Abstractions.Contracts;
using Norse.AuthN.Components.FluentUI.Manage;
using Norse.AuthN.Services;

namespace Norse.AuthN.Components.FluentUI.Tests.Manage;

public sealed class VerifyAuthenticatorFormTests : InteractiveBunitContext
{
	const string Invalid = "Verification code is invalid.";

	readonly ITwoFactorService _twoFactor = Substitute.For<ITwoFactorService>();
	readonly ISessionTransition _sessionTransition = Substitute.For<ISessionTransition>();

	public VerifyAuthenticatorFormTests()
	{
		Services.AddFluentUIComponents();
		// FluentUI components make JS interop calls bunit has no way to know about in advance —
		// loose mode is bunit's own documented answer (same rationale as LoginTests).
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services
			.AddSingleton(_twoFactor)
			.AddSingleton(_sessionTransition)
			.AddScoped<IValidator<EnableAuthenticatorRequest>, EnableAuthenticatorRequestValidator>();
	}

	BunitNavigationManager Navigation => Services.GetRequiredService<BunitNavigationManager>();

	static async Task FillAndSubmitAsync(IRenderedComponent<VerifyAuthenticatorForm> form, string code)
	{
		await form.Find("fluent-text-input").ChangeAsync(code);
		await form.Find("form").SubmitAsync();
	}

	static Outcome<NavigationResult> InvalidCode() =>
		Outcome<NavigationResult>.Err(ErrorCategory.Validation,
			new Dictionary<string, string[]> { [nameof(EnableAuthenticatorRequest.Code)] = [Invalid] });

	[Fact]
	void The_code_input_asks_for_a_one_time_code()
	{
		var form = Render<VerifyAuthenticatorForm>();

		form.Find("fluent-text-input").GetAttribute("autocomplete").ShouldBe("one-time-code");
	}

	[Fact]
	async Task An_empty_submit_never_reaches_the_service()
	{
		var form = Render<VerifyAuthenticatorForm>();

		await form.Find("form").SubmitAsync();

		await _twoFactor.DidNotReceiveWithAnyArgs().EnableAuthenticator(default!, Xunit.TestContext.Current.CancellationToken);
		_sessionTransition.DidNotReceiveWithAnyArgs().Begin(default!);
	}

	[Fact]
	async Task An_invalid_code_lands_on_the_field()
	{
		_twoFactor.EnableAuthenticator(Arg.Any<EnableAuthenticatorRequest>(), Arg.Any<CancellationToken>())
			.Returns(InvalidCode());
		var form = Render<VerifyAuthenticatorForm>();

		await FillAndSubmitAsync(form, "123456");

		// Field-scoped: rendered with the code input, never in the model-level summary.
		form.Markup.ShouldContain(Invalid);
		form.FindAll(".norse-model-errors").Where(e => e.TextContent.Contains(Invalid, StringComparison.Ordinal)).ShouldBeEmpty();
		_sessionTransition.DidNotReceiveWithAnyArgs().Begin(default!);
		Navigation.History.ShouldBeEmpty();
	}

	[Fact]
	async Task Success_begins_one_transition_with_the_server_url()
	{
		NavigationResult next = new() { NextUrl = "/Account/Manage/ShowRecoveryCodes?handoff=sealed" };
		_twoFactor.EnableAuthenticator(Arg.Any<EnableAuthenticatorRequest>(), Arg.Any<CancellationToken>())
			.Returns(Outcome<NavigationResult>.Ok(next));
		var form = Render<VerifyAuthenticatorForm>();

		// Raw as typed: the server strips spaces and hyphens, never the client.
		await FillAndSubmitAsync(form, "123 456");

		await _twoFactor.Received(1).EnableAuthenticator(Arg.Is<EnableAuthenticatorRequest>(r => r.Code == "123 456"),
			Arg.Any<CancellationToken>());
		_sessionTransition.Received(1).Begin(Arg.Is<NavigationResult>(r => r.NextUrl == "/Account/Manage/ShowRecoveryCodes?handoff=sealed"));
		Navigation.History.ShouldBeEmpty();
	}

	// The deadlock lock (same shape as LoginTests): a rejected code must be correctable — the
	// server-error store has to yield to the next validation pass, or the resubmit never dispatches.
	[Fact]
	async Task Reject_edit_resubmit_dispatches_twice()
	{
		NavigationResult next = new() { NextUrl = "/Account/Manage/TwoFactorAuthentication?status=authenticator-verified" };
		_twoFactor.EnableAuthenticator(Arg.Any<EnableAuthenticatorRequest>(), Arg.Any<CancellationToken>())
			.Returns(InvalidCode(), Outcome<NavigationResult>.Ok(next));
		var form = Render<VerifyAuthenticatorForm>();

		await FillAndSubmitAsync(form, "111111");
		form.Markup.ShouldContain(Invalid);

		await FillAndSubmitAsync(form, "222222");

		await _twoFactor.Received(2).EnableAuthenticator(Arg.Any<EnableAuthenticatorRequest>(), Arg.Any<CancellationToken>());
		form.Markup.ShouldNotContain(Invalid);
		_sessionTransition.Received(1).Begin(next);
	}
}
