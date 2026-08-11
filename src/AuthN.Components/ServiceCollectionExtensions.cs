using Microsoft.Extensions.DependencyInjection;

namespace Norse.AuthN.Components;

/// <summary>Registration entry point for the gate's session-transition seam.</summary>
public static class ServiceCollectionExtensions
{
	extension(IServiceCollection services)
	{
		/// <summary>
		///     Registers the production <see cref="ISessionTransition" /> — a forced document load at
		///     the server-resolved next hop. Scoped, matching
		///     <see cref="Microsoft.AspNetCore.Components.NavigationManager" />'s lifetime. Hosts that
		///     render the gate's components call this; the story catalog registers its recorder instead.
		/// </summary>
		/// <returns>The same service collection instance.</returns>
		public IServiceCollection AddNorseSessionTransition() =>
			services.AddScoped<ISessionTransition, ForceLoadSessionTransition>();
	}
}
