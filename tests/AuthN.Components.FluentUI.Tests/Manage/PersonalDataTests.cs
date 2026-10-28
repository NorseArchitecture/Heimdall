using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using Norse.AuthN.Components.FluentUI.Manage;
using Norse.AuthN.Services;
using Norse.Abstractions.Contracts;

namespace Norse.AuthN.Components.FluentUI.Tests.Manage;

public sealed class PersonalDataTests : BunitContext
{
	readonly IIdentityService _service = Substitute.For<IIdentityService>();

	public PersonalDataTests()
	{
		Services.AddFluentUIComponents();
		// FluentUI components make JS interop calls bunit has no way to know about in advance —
		// loose mode is bunit's own documented answer (same rationale as LoginTests), and it's also
		// what makes the download button's own JS-module import resolve to a harmless placeholder.
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddSingleton(_service);
		// Last: setting the renderer locks the service provider.
		SetRendererInfo(new RendererInfo("Server", isInteractive: true));
	}

	[Fact]
	void Renders_download_and_delete_affordances()
	{
		var component = Render<PersonalData>();

		component.Markup.ShouldContain("Personal Data");
		component.Markup.ShouldContain("Account/Manage/DeletePersonalData");
	}

	[Fact]
	async Task Download_click_calls_GetMyPersonalDataAsync()
	{
		_service.GetMyPersonalDataAsync(Arg.Any<GetMyPersonalDataRequest>(), Arg.Any<CancellationToken>())
			.Returns(_ =>
				Task.FromResult(
					Outcome<PersonalDataResponse>.Ok(new PersonalDataResponse
					{
						Email = "user@example.com",
						PhoneNumber = ""
					})));

		var component = Render<PersonalData>();
		await component.InvokeAsync(() => component.Find("fluent-button").Click());

		await _service.Received(1)
			.GetMyPersonalDataAsync(Arg.Any<GetMyPersonalDataRequest>(), Arg.Any<CancellationToken>());
	}

	// A second click mid-download would download twice: the button binds CanAct, so it is disabled while the
	// request is in flight and live again once it lands.
	[Fact]
	async Task The_download_is_disabled_while_it_is_in_flight()
	{
		TaskCompletionSource<Outcome<PersonalDataResponse>> pending = new();
		_service.GetMyPersonalDataAsync(Arg.Any<GetMyPersonalDataRequest>(), Arg.Any<CancellationToken>()).Returns(pending.Task);
		var component = Render<PersonalData>();

		var click = component.Find("fluent-button").ClickAsync(new());

		component.Find("fluent-button").HasAttribute("disabled").ShouldBeTrue();
		await component.InvokeAsync(() => pending.SetResult(Outcome<PersonalDataResponse>.Ok(new() { Email = "user@example.com", PhoneNumber = "" })));
		await click;
		component.Find("fluent-button").HasAttribute("disabled").ShouldBeFalse();
	}

	[Fact]
	async Task A_failed_download_renders_in_place()
	{
		_service.GetMyPersonalDataAsync(Arg.Any<GetMyPersonalDataRequest>(), Arg.Any<CancellationToken>())
			.Returns(Outcome<PersonalDataResponse>.Err(ErrorCategory.Fault, correlationId: Guid.Empty));
		var component = Render<PersonalData>();

		await component.Find("fluent-button").ClickAsync(new());

		component.Find("[role='alert']").TextContent.Trim().ShouldBe("Your personal data could not be downloaded.");
	}
}
