using Microsoft.AspNetCore.Authorization;
using Norse.Abstractions.Contracts;
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
	void Wire_records_carry_no_mediator_marker()
	{
		foreach (var wireType in (Type[])[typeof(LoginRequest), typeof(RegisterRequest), typeof(LoginResult), typeof(RegisterResult), typeof(LogoutResult)])
			wireType.GetInterfaces()
				.Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>))
				.ShouldBeFalse($"{wireType.Name} must not implement any closed IRequest<> — mediator identity is Himinbjörg's, never the wire's.");
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
