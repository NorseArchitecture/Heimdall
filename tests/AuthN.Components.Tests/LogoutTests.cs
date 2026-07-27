using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Norse.Abstractions.Contracts;
using Norse.AuthN.Services;

namespace Norse.AuthN.Components.Tests;

public sealed class LogoutTests : BunitContext
{
	[Fact]
	void Navigates_to_root_when_the_gateway_completes_sign_out_directly()
	{
		var service = Substitute.For<IAuthenticationService>();
		service.Logout(Arg.Any<LogoutRequest>(), Arg.Any<CancellationToken>())
			.Returns(_ => Task.FromResult(Outcome<LogoutResult>.Ok(new LogoutResult())));
		Services.AddSingleton(service);
		var navigation = Services.GetRequiredService<BunitNavigationManager>();

		Render<Logout>();

		navigation.Uri.ShouldBe(navigation.BaseUri);
		navigation.History.ShouldHaveSingleItem().Options.ForceLoad.ShouldBeTrue();
	}

	[Fact]
	void Navigates_to_the_deferred_completion_url_when_the_gateway_could_not_sign_out_directly()
	{
		var service = Substitute.For<IAuthenticationService>();
		service.Logout(Arg.Any<LogoutRequest>(), Arg.Any<CancellationToken>())
			.Returns(_ => Task.FromResult(Outcome<LogoutResult>.Ok(new LogoutResult { DeferredCompletionUrl = "/_auth/complete?key=abc&returnUrl=%2F" })));
		Services.AddSingleton(service);
		var navigation = Services.GetRequiredService<BunitNavigationManager>();

		Render<Logout>();

		navigation.Uri.ShouldBe(navigation.ToAbsoluteUri("/_auth/complete?key=abc&returnUrl=%2F").ToString());
		navigation.History.ShouldHaveSingleItem().Options.ForceLoad.ShouldBeTrue();
	}
}
