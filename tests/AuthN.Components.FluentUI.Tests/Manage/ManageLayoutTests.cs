using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using Norse.Abstractions.Components.Primitives;
using Norse.AuthN.Components.FluentUI.Manage;
using Norse.AuthN.Services;

namespace Norse.AuthN.Components.FluentUI.Tests.Manage;

public sealed class ManageLayoutTests : BunitContext
{
	public ManageLayoutTests()
	{
		Services.AddFluentUIComponents();
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddSingleton<IAppShellLayout, TestAppShell>();
	}

	[Fact]
	void Renders_the_app_shell_around_the_body()
	{
		var component = RenderLayout();

		component.Markup.ShouldContain("data-test-shell");
		component.Markup.ShouldContain("child");
		component.Markup.ShouldContain("Manage your account");
	}

	[Fact]
	void Renders_the_status_text_for_a_known_key()
	{
		Services.GetRequiredService<BunitNavigationManager>().NavigateTo("/Account/Manage?status=password-changed");

		var component = RenderLayout();

		component.Markup.ShouldContain("Your password has been changed.");
	}

	[Fact]
	void Renders_no_status_for_an_unknown_key()
	{
		Services.GetRequiredService<BunitNavigationManager>().NavigateTo("/Account/Manage?status=nope");

		var component = RenderLayout();

		component.FindAll("[role='alert']").ShouldBeEmpty();
	}

	// RouteView reads the page's [Layout] attribute and nests through LayoutView — the same resolution
	// the Yggdrasil router performs. A layout stamped with itself never returns from this render.
	[Fact]
	void A_manage_page_renders_through_the_router_layout_chain_exactly_once()
	{
		Services.AddSingleton(Substitute.For<IIdentityService>());
		RouteData routeData = new(typeof(PersonalData), new Dictionary<string, object?>());
		var cut = Render<RouteView>(p => p.Add(r => r.RouteData, routeData));
		cut.FindAll("[data-test-shell]").Count.ShouldBe(1);
		cut.FindAll("h1").Count(h => h.TextContent == "Manage your account").ShouldBe(1);
		cut.Markup.ShouldContain("Personal Data");
	}

	IRenderedComponent<ManageLayout> RenderLayout() =>
		Render<ManageLayout>(p => p.Add(l => l.Body, b => b.AddMarkupContent(0, "<p>child</p>")));
}
