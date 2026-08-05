using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using Norse.AuthN.Services;
using Norse.Abstractions.Contracts;

namespace Norse.AuthN.Components.FluentUI.Tests;

public sealed class PersonalDataTests : BunitContext
{
	public PersonalDataTests()
	{
		Services.AddFluentUIComponents();
		// FluentUI components make JS interop calls bunit has no way to know about in advance —
		// loose mode is bunit's own documented answer (same rationale as LoginTests), and it's also
		// what makes the download button's own JS-module import resolve to a harmless placeholder.
		JSInterop.Mode = JSRuntimeMode.Loose;
	}

	[Fact]
	void Renders_download_and_delete_affordances()
	{
		Services.AddSingleton(Substitute.For<IIdentityService>());

		var component = Render<PersonalData>();

		component.Markup.ShouldContain("Personal Data");
		component.Markup.ShouldContain("Account/Manage/DeletePersonalData");
	}

	[Fact]
	async Task Download_click_calls_GetMyPersonalDataAsync()
	{
		var service = Substitute.For<IIdentityService>();
		service.GetMyPersonalDataAsync(Arg.Any<GetMyPersonalDataRequest>(), Arg.Any<CancellationToken>())
			.Returns(_ => Task.FromResult(Outcome<PersonalDataResponse>.Ok(new PersonalDataResponse { Email = "user@example.com", PhoneNumber = "" })));
		Services.AddSingleton(service);

		var component = Render<PersonalData>();
		await component.InvokeAsync(() => component.Find("fluent-button").Click());

		await service.Received(1).GetMyPersonalDataAsync(Arg.Any<GetMyPersonalDataRequest>(), Arg.Any<CancellationToken>());
	}
}
