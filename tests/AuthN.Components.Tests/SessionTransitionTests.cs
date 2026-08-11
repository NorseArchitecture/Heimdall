using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Norse.AuthN.Components.Tests;

public sealed class SessionTransitionTests
{
	sealed class RecordingNavigationManager : NavigationManager
	{
		internal string? RequestedUri { get; private set; }
		internal NavigationOptions RequestedOptions { get; private set; }

		public RecordingNavigationManager() =>
			Initialize("http://localhost/", "http://localhost/");

		protected override void NavigateToCore(string uri, NavigationOptions options)
		{
			RequestedUri = uri;
			RequestedOptions = options;
		}
	}

	[Fact]
	void Begin_performs_a_forced_document_load_at_the_server_resolved_hop()
	{
		RecordingNavigationManager navigation = new();
		ForceLoadSessionTransition transition = new(navigation);

		transition.Begin(new() { NextUrl = "/Account/LoginWith2fa" });

		navigation.RequestedUri.ShouldBe("/Account/LoginWith2fa");
		navigation.RequestedOptions.ForceLoad.ShouldBeTrue();
	}

	[Fact]
	void AddNorseSessionTransition_registers_the_scoped_seam()
	{
		ServiceCollection services = new();

		services.AddNorseSessionTransition();

		services.ShouldContain(d => d.ServiceType == typeof(ISessionTransition)
			&& d.ImplementationType == typeof(ForceLoadSessionTransition)
			&& d.Lifetime == ServiceLifetime.Scoped);
	}
}
