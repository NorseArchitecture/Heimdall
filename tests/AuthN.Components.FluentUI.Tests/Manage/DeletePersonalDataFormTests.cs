using Bunit;
using Bunit.TestDoubles;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using Norse.Abstractions.Contracts;
using Norse.AuthN.Components.FluentUI.Manage;
using Norse.AuthN.Services;

namespace Norse.AuthN.Components.FluentUI.Tests.Manage;

public sealed class DeletePersonalDataFormTests : BunitContext
{
	readonly IAccountService _account = Substitute.For<IAccountService>();
	readonly ISessionTransition _sessionTransition = Substitute.For<ISessionTransition>();

	public DeletePersonalDataFormTests()
	{
		Services.AddFluentUIComponents();
		// FluentUI components make JS interop calls bunit has no way to know about in advance —
		// loose mode is bunit's own documented answer (same rationale as LoginTests).
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services
			.AddSingleton(_account)
			.AddSingleton(_sessionTransition)
			.AddScoped<IValidator<DeletePersonalDataRequest>, DeletePersonalDataRequestValidator>();
	}

	BunitNavigationManager Navigation => Services.GetRequiredService<BunitNavigationManager>();

	IRenderedComponent<DeletePersonalDataForm> RenderForm(bool hasPassword) =>
		Render<DeletePersonalDataForm>(parameters => parameters.Add(p => p.HasPassword, hasPassword));

	static async Task FillAndSubmitAsync(IRenderedComponent<DeletePersonalDataForm> form, string password)
	{
		await form.Find("fluent-text-input").ChangeAsync(password);
		await form.Find("form").SubmitAsync();
	}

	void ReturnsFromDelete(Outcome<NavigationResult> outcome) =>
		_account.DeletePersonalData(Arg.Any<DeletePersonalDataRequest>(), Arg.Any<CancellationToken>()).Returns(outcome);

	[Fact]
	async Task A_passwordless_submit_reaches_the_service_with_an_empty_password()
	{
		ReturnsFromDelete(Outcome<NavigationResult>.Ok(new() { NextUrl = "/" }));
		var form = RenderForm(hasPassword: false);

		await form.Find("form").SubmitAsync();

		await _account.Received(1).DeletePersonalData(
			Arg.Is<DeletePersonalDataRequest>(r => r.Password == ""),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	async Task Success_begins_one_transition_and_no_history()
	{
		NavigationResult next = new() { NextUrl = "/" };
		ReturnsFromDelete(Outcome<NavigationResult>.Ok(next));
		var form = RenderForm(hasPassword: true);

		await FillAndSubmitAsync(form, "correct horse 1");

		await _account.Received(1).DeletePersonalData(
			Arg.Is<DeletePersonalDataRequest>(r => r.Password == "correct horse 1"),
			Arg.Any<CancellationToken>());
		_sessionTransition.Received(1).Begin(next);
		Navigation.History.ShouldBeEmpty();
	}

	[Fact]
	async Task A_wrong_password_renders_in_the_summary_without_a_transition()
	{
		const string Incorrect = "Incorrect password.";
		ReturnsFromDelete(new(new Failed(Problem.ModelError(ErrorCategory.InvalidCredentials, Incorrect))));
		var form = RenderForm(hasPassword: true);

		await FillAndSubmitAsync(form, "wrong horse 1");

		form.Find(".norse-model-errors").TextContent.ShouldContain(Incorrect);
		_sessionTransition.DidNotReceiveWithAnyArgs().Begin(default!);
		Navigation.History.ShouldBeEmpty();
	}

	[Fact]
	async Task A_shred_failure_renders_the_administrator_wording()
	{
		const string ShredFailed =
			"Your account could not be fully deleted. The failure has been logged for an administrator to finish.";
		ReturnsFromDelete(new(new Failed(Problem.ModelError(ErrorCategory.Fault, ShredFailed))));
		var form = RenderForm(hasPassword: true);

		await FillAndSubmitAsync(form, "correct horse 1");

		form.Find(".norse-model-errors").TextContent.ShouldContain(ShredFailed);
		_sessionTransition.DidNotReceiveWithAnyArgs().Begin(default!);
		Navigation.History.ShouldBeEmpty();
	}
}
