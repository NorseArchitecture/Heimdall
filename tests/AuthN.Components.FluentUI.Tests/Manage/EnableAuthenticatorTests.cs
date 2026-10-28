using System.Text;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using Norse.Abstractions.Contracts;
using Norse.AuthN.Components.FluentUI.Manage;
using Norse.AuthN.Services;

namespace Norse.AuthN.Components.FluentUI.Tests.Manage;

public sealed class EnableAuthenticatorTests : InteractiveBunitContext
{
	const string Svg = """<svg xmlns="http://www.w3.org/2000/svg"><rect width="1" height="1"/></svg>""";

	readonly ITwoFactorService _twoFactor = Substitute.For<ITwoFactorService>();

	public EnableAuthenticatorTests()
	{
		Services.AddFluentUIComponents();
		// FluentUI components make JS interop calls bunit has no way to know about in advance —
		// loose mode is bunit's own documented answer (same rationale as LoginTests).
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services
			.AddSingleton(_twoFactor)
			.AddSingleton(Substitute.For<ISessionTransition>());
	}

	[Fact]
	void The_shared_key_and_qr_render_from_the_setup()
	{
		_twoFactor.GetAuthenticatorSetup(Arg.Any<CancellationToken>())
			.Returns(Outcome<AuthenticatorSetupResponse>.Ok(new()
			{
				SharedKey = "abcd efgh ijkl mnop",
				AuthenticatorUri = "otpauth://totp/Norse:user@example.com?secret=ABCDEFGHIJKLMNOP&issuer=Norse&digits=6",
				QrCodeSvg = Svg,
			}));

		var page = Render<EnableAuthenticator>();

		page.Find("kbd").TextContent.ShouldBe("abcd efgh ijkl mnop");
		var image = page.Find("img");
		image.GetAttribute("alt").ShouldBe("QR code for your authenticator app");
		var source = image.GetAttribute("src").ShouldNotBeNull();
		source.ShouldStartWith("data:image/svg+xml;base64,");
		Encoding.UTF8.GetString(Convert.FromBase64String(source["data:image/svg+xml;base64,".Length..])).ShouldBe(Svg);
		// An image cannot run script; inline markup could.
		page.FindAll("svg").ShouldBeEmpty();
		page.FindComponents<VerifyAuthenticatorForm>().ShouldHaveSingleItem();
	}

	[Fact]
	void A_failed_setup_load_renders_the_problem_and_no_form()
	{
		_twoFactor.GetAuthenticatorSetup(Arg.Any<CancellationToken>())
			.Returns(Outcome<AuthenticatorSetupResponse>.Err(ErrorCategory.Fault, correlationId: Guid.Empty));

		var page = Render<EnableAuthenticator>();

		page.Find("[role=alert]").TextContent.ShouldContain("Your authenticator setup could not be loaded.");
		page.FindComponents<VerifyAuthenticatorForm>().ShouldBeEmpty();
		page.FindAll("form").ShouldBeEmpty();
		page.FindAll("img").ShouldBeEmpty();
	}
}
