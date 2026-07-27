using Microsoft.AspNetCore.Authorization;
using Norse.Abstractions.Contracts;
using System.Reflection;

namespace Norse.AuthN.Services.Tests;

public sealed class RequestContractTests
{
	[Fact]
	void Every_request_is_a_marked_command_with_a_declared_policy()
	{
		typeof(ICommandRequest<BoolResponse>).IsAssignableFrom(typeof(LoginRequest)).ShouldBeTrue();
		typeof(ICommandRequest<BoolResponse>).IsAssignableFrom(typeof(RegisterRequest)).ShouldBeTrue();
		typeof(ICommandRequest<Unit>).IsAssignableFrom(typeof(LogoutRequest)).ShouldBeTrue();

		foreach (var request in (Type[])[typeof(LoginRequest), typeof(RegisterRequest), typeof(LogoutRequest)])
			request.GetCustomAttribute<AuthorizeAttribute>()!.Policy.ShouldBe(AuthNPolicies.Public);
	}

	[Fact]
	void Every_service_method_takes_a_trailing_cancellation_token()
	{
		foreach (var method in typeof(IAuthenticationService).GetMethods())
			method.GetParameters()[^1].ParameterType.ShouldBe(typeof(CancellationToken));
	}
}
