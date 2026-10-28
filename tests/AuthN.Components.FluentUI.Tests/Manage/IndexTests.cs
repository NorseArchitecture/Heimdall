using Bunit;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using Norse.Abstractions.Contracts;
using Norse.AuthN.Components.FluentUI.Manage;
using Norse.AuthN.Services;
using Index = Norse.AuthN.Components.FluentUI.Manage.Index;

namespace Norse.AuthN.Components.FluentUI.Tests.Manage;

public sealed class IndexTests : InteractiveBunitContext
{
	readonly IIdentityService _identity = Substitute.For<IIdentityService>();

	public IndexTests()
	{
		Services.AddFluentUIComponents();
		// FluentUI components make JS interop calls bunit has no way to know about in advance —
		// loose mode is bunit's own documented answer (same rationale as LoginTests).
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services
			.AddSingleton(_identity)
			.AddSingleton(Substitute.For<IAccountService>())
			.AddScoped<IValidator<SetPhoneNumberRequest>, SetPhoneNumberRequestValidator>()
			.AddSingleton(Substitute.For<ISessionTransition>());
	}

	[Fact]
	void Loads_the_profile_on_initialization_and_renders_the_form()
	{
		_identity.GetMyPersonalDataAsync(Arg.Any<GetMyPersonalDataRequest>(), Arg.Any<CancellationToken>())
			.Returns(Outcome<PersonalDataResponse>.Ok(new() { Email = "user@example.com", PhoneNumber = "+15551234567" }));

		var page = Render<Index>();

		// FormName renders nothing under bUnit's interactive render, and the placeholder already carries the
		// sample number, so the markup is no witness — the rendered form's own parameters are.
		var form = page.FindComponent<ProfileForm>().Instance;
		form.Email.ShouldBe("user@example.com");
		form.PhoneNumber.ShouldBe("+15551234567");
	}

	[Fact]
	void A_failed_load_renders_the_problem_and_no_form()
	{
		_identity.GetMyPersonalDataAsync(Arg.Any<GetMyPersonalDataRequest>(), Arg.Any<CancellationToken>())
			.Returns(Outcome<PersonalDataResponse>.Err(ErrorCategory.Fault, correlationId: Guid.Empty));

		var page = Render<Index>();

		page.Markup.ShouldContain("could not be loaded");
		page.FindComponents<ProfileForm>().ShouldBeEmpty();
	}
}
