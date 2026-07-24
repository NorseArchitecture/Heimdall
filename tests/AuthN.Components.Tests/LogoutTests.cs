using System.Diagnostics.CodeAnalysis;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Norse.Abstractions.Contracts;
using Norse.AuthN.Services;

namespace Norse.AuthN.Components.Tests;

public sealed class LogoutTests : BunitContext
{
	[Fact]
	[SuppressMessage("Usage", "CA2012:Use ValueTasks correctly")]
	void Navigates_to_root_when_the_gateway_completes_sign_out_directly()
	{
		var gateway = Substitute.For<IAuthenticationGateway>();
		gateway.Logout(Arg.Any<LogoutRequest>(), Arg.Any<CancellationToken>())
			.Returns(_ => ValueTask.FromResult(Outcome<LogoutResult>.Ok(new LogoutResult())));
		Services.AddSingleton(gateway);
		var navigation = Services.GetRequiredService<BunitNavigationManager>();

		Render<Logout>();

		navigation.Uri.ShouldBe(navigation.BaseUri);
		navigation.History.ShouldHaveSingleItem().Options.ForceLoad.ShouldBeTrue();
	}

	[Fact]
	[SuppressMessage("Usage", "CA2012:Use ValueTasks correctly")]
	void Navigates_to_the_deferred_completion_url_when_the_gateway_could_not_sign_out_directly()
	{
		var gateway = Substitute.For<IAuthenticationGateway>();
		gateway.Logout(Arg.Any<LogoutRequest>(), Arg.Any<CancellationToken>())
			.Returns(_ => ValueTask.FromResult(Outcome<LogoutResult>.Ok(new LogoutResult { DeferredCompletionUrl = "/_auth/complete?key=abc&returnUrl=%2F" })));
		Services.AddSingleton(gateway);
		var navigation = Services.GetRequiredService<BunitNavigationManager>();

		Render<Logout>();

		navigation.Uri.ShouldBe(navigation.ToAbsoluteUri("/_auth/complete?key=abc&returnUrl=%2F").ToString());
		navigation.History.ShouldHaveSingleItem().Options.ForceLoad.ShouldBeTrue();
	}
}
