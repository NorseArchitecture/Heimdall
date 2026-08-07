using Bunit;
using FluentValidation;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.FluentUI.AspNetCore.Components;
using Norse.AuthN.Services;

namespace Norse.AuthN.Components.FluentUI.Tests;

public sealed class GateLayoutTests : BunitContext
{
	public GateLayoutTests()
	{
		Services.AddFluentUIComponents();
		// GateLayout hosts FluentUI-backed pages via @Body; loose JS interop mode matches every
		// other FluentUI-touching test in this project (see LoginTests/RegisterTests).
		JSInterop.Mode = JSRuntimeMode.Loose;
	}

	[Fact]
	void Renders_the_identity_panel_copy()
	{
		var component = RenderGate();

		component.Markup.ShouldContain("Heimdall keeps the gate.");
		component.Markup.ShouldContain("NORSE ARCHITECTURE");
		component.Markup.ShouldContain("norse_identity · OpenIddict · OAuth 2.1");
	}

	[Fact]
	void Renders_the_prismatic_seam_element()
	{
		var component = RenderGate();

		component.Find(".gate-seam").GetAttribute("aria-hidden").ShouldBe("true");
	}

	[Fact]
	void Renders_the_body_content_inside_the_form_column()
	{
		var component = RenderGate();

		component.Find(".gate-form").TextContent.ShouldContain("gate-body-marker");
	}

	[Fact]
	void Login_renders_the_create_an_account_link()
	{
		Services.AddSingleton(Substitute.For<IAuthenticationService>());
		Services.AddScoped<IValidator<LoginRequest>, LoginRequestValidator>();

		var component = Render<Login>();

		var createAccount = component.Find("a[href='Account/Register']");
		createAccount.TextContent.ShouldBe("Create an account");
	}

	// FluentField renders Parameters.Label then Parameters.LabelTemplate into the same
	// <label slot="label"> element (confirmed by decompiling FluentField.BuildRenderTree) — passing
	// the link as the Password field's LabelTemplate is what actually puts it "beside the label",
	// per spec, rather than as a block-level sibling after the field. Asserts the link's real
	// ancestor, not just its presence/href, so a regression back to a sibling placement fails here.
	[Fact]
	void Login_places_the_forgot_password_link_inside_the_password_fields_label()
	{
		Services.AddSingleton(Substitute.For<IAuthenticationService>());
		Services.AddScoped<IValidator<LoginRequest>, LoginRequestValidator>();

		var component = Render<Login>();

		var forgotPassword = component.Find("a[href='Account/ForgotPassword']");
		forgotPassword.TextContent.ShouldBe("Forgot password?");

		var enclosingLabel = forgotPassword.Closest("label[slot='label']");

		enclosingLabel.ShouldNotBeNull();
		enclosingLabel.TextContent.ShouldContain("Password");
	}

	[Fact]
	void Register_renders_the_already_have_an_account_link()
	{
		var service = Substitute.For<IAuthenticationService>();
		Services.AddSingleton(service);
		Services.AddScoped<IValidator<RegisterRequest>>(_ => new RegisterRequestValidator(service, NullLogger<RegisterRequestValidator>.Instance));

		var component = Render<Register>();

		component.Markup.ShouldContain("Already have an account?");

		var logIn = component.Find("a[href='Account/Login']");
		logIn.TextContent.ShouldBe("Log in");
	}

	IRenderedComponent<GateLayout> RenderGate() =>
		Render<GateLayout>(parameters => parameters.Add(p => p.Body, (RenderFragment)(builder => builder.AddContent(0, "gate-body-marker"))));
}
