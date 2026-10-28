using Bunit;
using Bunit.TestDoubles;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using Norse.Abstractions.Contracts;
using Norse.AuthN.Components.FluentUI.Manage;
using Norse.AuthN.Services;

namespace Norse.AuthN.Components.FluentUI.Tests.Manage;

public sealed class RenamePasskeyFormTests : InteractiveBunitContext
{
	readonly IPasskeyService _passkeys = Substitute.For<IPasskeyService>();

	public RenamePasskeyFormTests()
	{
		Services.AddFluentUIComponents();
		// FluentUI components make JS interop calls bunit has no way to know about in advance —
		// loose mode is bunit's own documented answer (same rationale as LoginTests).
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services
			.AddSingleton(_passkeys)
			.AddScoped<IValidator<RenamePasskeyRequest>, RenamePasskeyRequestValidator>();
	}

	BunitNavigationManager Navigation => Services.GetRequiredService<BunitNavigationManager>();

	IRenderedComponent<RenamePasskeyForm> RenderForm() =>
		Render<RenamePasskeyForm>(p => p.Add(f => f.CredentialId, "a").Add(f => f.CurrentName, "Laptop"));

	static async Task RenameAndSubmitAsync(IRenderedComponent<RenamePasskeyForm> form, string name)
	{
		await form.Find("fluent-text-input").ChangeAsync(name);
		await form.Find("form").SubmitAsync();
	}

	void ReturnsFromRename(Outcome<NavigationResult> outcome) =>
		_passkeys.RenamePasskey(Arg.Any<RenamePasskeyRequest>(), Arg.Any<CancellationToken>()).Returns(outcome);

	[Fact]
	async Task An_empty_name_never_reaches_the_service()
	{
		var form = RenderForm();

		await RenameAndSubmitAsync(form, "");

		await _passkeys.DidNotReceiveWithAnyArgs().RenamePasskey(default!, Xunit.TestContext.Current.CancellationToken);
	}

	[Fact]
	async Task A_201_character_name_never_reaches_the_service()
	{
		var form = RenderForm();

		await RenameAndSubmitAsync(form, new('x', 201));

		await _passkeys.DidNotReceiveWithAnyArgs().RenamePasskey(default!, Xunit.TestContext.Current.CancellationToken);
	}

	[Fact]
	async Task Success_navigates_softly_to_the_server_url()
	{
		const string Next = "/Account/Manage/Passkeys?status=passkey-updated";
		ReturnsFromRename(Outcome<NavigationResult>.Ok(new() { NextUrl = Next }));
		var form = RenderForm();

		await RenameAndSubmitAsync(form, "Work laptop");

		await _passkeys.Received(1).RenamePasskey(
			Arg.Is<RenamePasskeyRequest>(r => r.CredentialId == "a" && r.Name == "Work laptop"),
			Arg.Any<CancellationToken>());
		var entry = Navigation.History.ShouldHaveSingleItem();
		entry.Uri.ShouldBe(Next);
		entry.Options.ForceLoad.ShouldBeFalse();
	}

	[Fact]
	async Task A_missing_passkey_renders_in_the_summary()
	{
		const string NotFound = "The passkey was not found.";
		ReturnsFromRename(new(new Failed(Problem.ModelError(ErrorCategory.NotFound, NotFound))));
		var form = RenderForm();

		await RenameAndSubmitAsync(form, "Work laptop");

		form.Find(".norse-model-errors").TextContent.ShouldContain(NotFound);
		Navigation.History.ShouldBeEmpty();
	}
}
