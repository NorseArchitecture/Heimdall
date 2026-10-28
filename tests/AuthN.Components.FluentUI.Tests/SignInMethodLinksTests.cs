using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Norse.Abstractions.Contracts;
using Norse.AuthN.Services;

namespace Norse.AuthN.Components.FluentUI.Tests;

public sealed class SignInMethodLinksTests : BunitContext
{
	// Deliberately shaped so no client composition could produce them: a PathBase prefix, a carried
	// return URL, and a provider start URL with its own query — only a verbatim copy matches.
	static readonly SignInMethodsResponse _methods = new()
	{
		PasswordSignInUrl = "/app/Account/Login?returnUrl=%2Fapp%2Forders",
		PasskeySignInUrl = "/app/Account/LoginWithPasskey?returnUrl=%2Fapp%2Forders",
		Providers =
		[
			new() { DisplayName = "Contoso", StartUrl = "/app/Account/PerformExternalLogin?provider=Contoso&returnUrl=%2Fapp%2Forders" },
			new() { DisplayName = "Fabrikam", StartUrl = "/app/Account/PerformExternalLogin?provider=Fabrikam&returnUrl=%2Fapp%2Forders" },
		],
	};

	readonly ISignInMethodsService _service = Substitute.For<ISignInMethodsService>();

	public SignInMethodLinksTests() =>
		Services.AddSingleton(_service);

	[Fact]
	void Every_link_is_the_servers_url_verbatim()
	{
		Returns(_methods);

		var links = RenderLinks("/app/orders", showPassword: true, showPasskey: true);

		links.FindAll("a").Select(a => a.GetAttribute("href")).ShouldBe(
		[
			_methods.PasskeySignInUrl,
			_methods.PasswordSignInUrl,
			_methods.Providers[0].StartUrl,
			_methods.Providers[1].StartUrl,
		]);
		_service.Received(1).GetSignInMethods(Arg.Is<GetSignInMethodsRequest>(r => r.ReturnUrl == "/app/orders"), Arg.Any<CancellationToken>());
	}

	// target="_top" keeps the interactive router from intercepting the click: each StartUrl is an HTTP
	// endpoint (a challenge), which only a full page load reaches.
	[Fact]
	void Each_provider_renders_one_top_level_link()
	{
		Returns(_methods);

		var links = RenderLinks("/app/orders", showPassword: false, showPasskey: false);

		var contoso = links.FindAll($"a[href='{_methods.Providers[0].StartUrl}']").ShouldHaveSingleItem();
		contoso.GetAttribute("target").ShouldBe("_top");
		contoso.TextContent.Trim().ShouldBe("Sign in with Contoso");
		var fabrikam = links.FindAll($"a[href='{_methods.Providers[1].StartUrl}']").ShouldHaveSingleItem();
		fabrikam.GetAttribute("target").ShouldBe("_top");
		fabrikam.TextContent.Trim().ShouldBe("Sign in with Fabrikam");
	}

	[Fact]
	void No_providers_renders_no_provider_section()
	{
		Returns(_methods with { Providers = [] });

		var links = RenderLinks("/app/orders", showPassword: true, showPasskey: true);

		links.FindAll(".sign-in-providers").ShouldBeEmpty();
		links.FindAll("a[target='_top']").ShouldBeEmpty();
	}

	[Theory]
	[InlineData(false, false)]
	[InlineData(true, false)]
	[InlineData(false, true)]
	[InlineData(true, true)]
	void Show_flags_select_the_password_and_passkey_links(bool showPassword, bool showPasskey)
	{
		Returns(_methods with { Providers = [] });

		var links = RenderLinks("/app/orders", showPassword, showPasskey);

		links.FindAll($"a[href='{_methods.PasswordSignInUrl}']").Count.ShouldBe(showPassword ? 1 : 0);
		links.FindAll($"a[href='{_methods.PasskeySignInUrl}']").Count.ShouldBe(showPasskey ? 1 : 0);
		if (showPassword)
			links.Find($"a[href='{_methods.PasswordSignInUrl}']").TextContent.Trim().ShouldBe("Use a password");
		if (showPasskey)
			links.Find($"a[href='{_methods.PasskeySignInUrl}']").TextContent.Trim().ShouldBe("Use a passkey");
	}

	[Fact]
	void A_failed_load_renders_the_problem_and_no_links()
	{
		_service.GetSignInMethods(Arg.Any<GetSignInMethodsRequest>(), Arg.Any<CancellationToken>())
			.Returns(Outcome<SignInMethodsResponse>.Err(ErrorCategory.Fault));

		var links = RenderLinks("/app/orders", showPassword: true, showPasskey: true);

		links.FindAll("a").ShouldBeEmpty();
		links.Find("[role='alert']").TextContent.Trim().ShouldBe("Other sign-in methods could not be loaded.");
	}

	[Fact]
	void The_methods_load_once_per_return_url()
	{
		Returns(_methods);
		var links = RenderLinks("/app/orders", showPassword: true, showPasskey: true);

		links.Render(p => p.Add(c => c.ShowPassword, false));
		links.Render(p => p.Add(c => c.ReturnUrl, "/app/invoices"));

		_service.Received(1).GetSignInMethods(Arg.Is<GetSignInMethodsRequest>(r => r.ReturnUrl == "/app/orders"), Arg.Any<CancellationToken>());
		_service.Received(1).GetSignInMethods(Arg.Is<GetSignInMethodsRequest>(r => r.ReturnUrl == "/app/invoices"), Arg.Any<CancellationToken>());
	}

	// Asgard's DispatchAsync drops a dispatch that overlaps one in flight: a ReturnUrl change mid-load starts nothing
	// by itself. The load in flight completes with the old URL's links, which must never render, and the component
	// re-dispatches for the current URL, whose links end up rendered.
	[Fact]
	void A_return_url_change_mid_load_renders_the_current_urls_links()
	{
		TaskCompletionSource<Outcome<SignInMethodsResponse>> gate = new();
		var invoices = _methods with
		{
			PasswordSignInUrl = "/app/Account/Login?returnUrl=%2Fapp%2Finvoices",
			PasskeySignInUrl = "/app/Account/LoginWithPasskey?returnUrl=%2Fapp%2Finvoices",
			Providers = [],
		};
		_service.GetSignInMethods(Arg.Is<GetSignInMethodsRequest>(r => r.ReturnUrl == "/app/orders"), Arg.Any<CancellationToken>())
			.Returns(gate.Task);
		_service.GetSignInMethods(Arg.Is<GetSignInMethodsRequest>(r => r.ReturnUrl == "/app/invoices"), Arg.Any<CancellationToken>())
			.Returns(Outcome<SignInMethodsResponse>.Ok(invoices));
		var links = RenderLinks("/app/orders", showPassword: true, showPasskey: true);

		links.Render(p => p.Add(c => c.ReturnUrl, "/app/invoices"));
		gate.SetResult(Outcome<SignInMethodsResponse>.Ok(_methods));

		links.WaitForAssertion(() => links.FindAll("a").Select(a => a.GetAttribute("href")).ShouldBe(
		[
			invoices.PasskeySignInUrl,
			invoices.PasswordSignInUrl,
		]));
		_service.Received(1).GetSignInMethods(Arg.Is<GetSignInMethodsRequest>(r => r.ReturnUrl == "/app/invoices"), Arg.Any<CancellationToken>());
	}

	void Returns(SignInMethodsResponse response) =>
		_service.GetSignInMethods(Arg.Any<GetSignInMethodsRequest>(), Arg.Any<CancellationToken>())
			.Returns(Outcome<SignInMethodsResponse>.Ok(response));

	IRenderedComponent<SignInMethodLinks> RenderLinks(string? returnUrl, bool showPassword, bool showPasskey) =>
		Render<SignInMethodLinks>(p => p
			.Add(c => c.ReturnUrl, returnUrl)
			.Add(c => c.ShowPassword, showPassword)
			.Add(c => c.ShowPasskey, showPasskey));
}
