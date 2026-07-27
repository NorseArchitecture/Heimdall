using Microsoft.AspNetCore.Authorization;
using System.Reflection;

namespace Norse.AuthN.Services.Tests;

/// <summary>
/// Purity test — locks the ratified wire-purity ruling against future drift. Wire
/// <c>[DataContract]</c> records carry no mediator marker and no <c>[Authorize]</c> at all; those
/// belong solely to Himinbjörg's server-sovereign command wrappers.
/// </summary>
public sealed class RequestContractTests
{
	[Fact]
	void AuthN_Services_does_not_reference_the_mediator_law_assembly()
	{
		// The mediator marker family (IRequest<T>/ICommandRequest<T>/IQueryRequest<T>) moved into
		// Norse.Abstractions.Web.Server (server-only law, 2026-07-27 amendment) precisely so a wire
		// assembly could no longer even name them, let alone implement them. Asserting a wire type
		// "doesn't implement IRequest<>" is no longer expressible from here — this assembly cannot
		// reference the type at all — so the stronger, structural check is that the assembly
		// reference itself is absent. That absence is the actual enforcement mechanism.
		typeof(IAuthenticationService).Assembly.GetReferencedAssemblies()
			.Any(a => a.Name == "Norse.Abstractions.Web.Server")
			.ShouldBeFalse("Norse.AuthN.Services must not reference Norse.Abstractions.Web.Server — mediator law is structurally invisible to the wire assembly, not merely unused by convention.");
	}

	[Fact]
	void Wire_records_carry_no_Authorize_attribute()
	{
		foreach (var wireType in (Type[])[typeof(LoginRequest), typeof(RegisterRequest), typeof(LoginResult), typeof(RegisterResult), typeof(LogoutResult)])
			wireType.GetCustomAttribute<AuthorizeAttribute>()
				.ShouldBeNull($"{wireType.Name} must not carry [Authorize] — that policy lives on Himinbjörg's command wrapper.");
	}

	[Fact]
	void Every_service_method_ends_with_a_trailing_cancellation_token()
	{
		foreach (var method in typeof(IAuthenticationService).GetMethods())
			method.GetParameters()[^1].ParameterType.ShouldBe(typeof(CancellationToken));
	}
}
