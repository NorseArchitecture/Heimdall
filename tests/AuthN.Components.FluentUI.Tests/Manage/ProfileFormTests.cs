using Bunit;
using Bunit.TestDoubles;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using Norse.Abstractions.Contracts;
using Norse.AuthN.Components.FluentUI.Manage;
using Norse.AuthN.Services;

namespace Norse.AuthN.Components.FluentUI.Tests.Manage;

public sealed class ProfileFormTests : BunitContext
{
	readonly IAccountService _account = Substitute.For<IAccountService>();
	readonly ISessionTransition _sessionTransition = Substitute.For<ISessionTransition>();

	public ProfileFormTests()
	{
		Services.AddFluentUIComponents();
		// FluentUI components make JS interop calls bunit has no way to know about in advance —
		// loose mode is bunit's own documented answer (same rationale as LoginTests).
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services
			.AddSingleton(_account)
			.AddScoped<IValidator<SetPhoneNumberRequest>, SetPhoneNumberRequestValidator>()
			.AddSingleton(_sessionTransition);
	}

	BunitNavigationManager Navigation => Services.GetRequiredService<BunitNavigationManager>();

	IRenderedComponent<ProfileForm> RenderForm() =>
		Render<ProfileForm>(p => p.Add(f => f.Email, "user@example.com").Add(f => f.PhoneNumber, "+15551234567"));

	static async Task ChangePhoneAndSubmitAsync(IRenderedComponent<ProfileForm> form, string phone)
	{
		await form.FindAll("fluent-text-input")[1].ChangeAsync(phone);
		await form.Find("form").SubmitAsync();
	}

	[Fact]
	async Task A_malformed_phone_never_reaches_the_service()
	{
		var form = RenderForm();

		await ChangePhoneAndSubmitAsync(form, "5551234567");

		await _account.DidNotReceiveWithAnyArgs().SetPhoneNumber(default!, Xunit.TestContext.Current.CancellationToken);
	}

	[Fact]
	async Task An_empty_phone_is_sent_as_a_clear()
	{
		_account.SetPhoneNumber(Arg.Any<SetPhoneNumberRequest>(), Arg.Any<CancellationToken>())
			.Returns(Outcome<NavigationResult>.Ok(new() { NextUrl = "/Account/Manage?status=phone-updated" }));
		var form = RenderForm();

		await ChangePhoneAndSubmitAsync(form, "");

		await _account.Received(1).SetPhoneNumber(
			Arg.Is<SetPhoneNumberRequest>(r => r.PhoneInput == "" && !r.Phone.HasValue),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	async Task Success_begins_one_transition_and_no_history()
	{
		_account.SetPhoneNumber(Arg.Any<SetPhoneNumberRequest>(), Arg.Any<CancellationToken>())
			.Returns(Outcome<NavigationResult>.Ok(new() { NextUrl = "/Account/Manage?status=phone-updated" }));
		var form = RenderForm();

		await ChangePhoneAndSubmitAsync(form, "+15557654321");

		_sessionTransition.Received(1).Begin(Arg.Is<NavigationResult>(r => r.NextUrl == "/Account/Manage?status=phone-updated"));
		Navigation.History.ShouldBeEmpty();
	}

	[Fact]
	async Task A_failed_save_renders_in_the_summary_without_a_transition()
	{
		_account.SetPhoneNumber(Arg.Any<SetPhoneNumberRequest>(), Arg.Any<CancellationToken>())
			.Returns(Outcome<NavigationResult>.Err(ErrorCategory.Fault, correlationId: Guid.Empty));
		var form = RenderForm();

		await ChangePhoneAndSubmitAsync(form, "+15557654321");

		form.Markup.ShouldContain("Something went wrong.");
		_sessionTransition.DidNotReceive().Begin(Arg.Any<NavigationResult>());
		Navigation.History.ShouldBeEmpty();
	}
}
