using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Norse.Abstractions.Components.Authorization;

namespace Norse.AuthN.Services.Tests;

public sealed class PolicyDeclarationTests
{
	static MethodInfo Declaration(Type owner, string name) =>
		owner.GetMethods(BindingFlags.Public | BindingFlags.Static)
			.Single(m => m.GetCustomAttribute<NorsePolicyAttribute>()?.Name == name);

	static AuthorizationPolicy Build(Type owner, string name)
	{
		AuthorizationPolicyBuilder builder = new();
		Declaration(owner, name).Invoke(null, [builder]);
		return builder.Build();
	}

	static AuthorizationResult Evaluate(string policyName, Type owner, ClaimsPrincipal principal)
	{
		var policy = Build(owner, policyName);
		using var provider = new ServiceCollection()
			.AddAuthorizationCore(options => options.AddPolicy(policyName, policy))
			.AddLogging()
			.BuildServiceProvider();
		return provider.GetRequiredService<IAuthorizationService>()
			.AuthorizeAsync(principal, null, policyName)
			.GetAwaiter()
			.GetResult();
	}

	static ClaimsPrincipal Principal(string role) =>
		new(new ClaimsIdentity([new Claim(ClaimTypes.Role, role)], "Test"));

	[Fact]
	void AuthN_declares_its_public_policy_in_metadata() =>
		Should.NotThrow(() => Declaration(typeof(AuthNPolicies), AuthNPolicies.Public));

	[Fact]
	void The_public_policy_now_requires_a_principal_rather_than_asserting_true() =>
		Build(typeof(AuthNPolicies), AuthNPolicies.Public).Requirements
			.ShouldContain(r => r is DenyAnonymousAuthorizationRequirement);

	[Fact]
	void Identity_declares_both_of_its_policies_in_metadata()
	{
		Should.NotThrow(() => Declaration(typeof(IdentityPolicies), IdentityPolicies.Self));
		Should.NotThrow(() => Declaration(typeof(IdentityPolicies), IdentityPolicies.MaskedDisclosure));
	}

	[Fact]
	void Masked_disclosure_still_requires_the_system_role() =>
		Build(typeof(IdentityPolicies), IdentityPolicies.MaskedDisclosure).Requirements
			.ShouldContain(r => r is RolesAuthorizationRequirement);

	[Fact]
	void Every_declaration_in_this_realm_carries_the_generator_visible_signature()
	{
		foreach (var owner in new[] { typeof(AuthNPolicies), typeof(IdentityPolicies) })
		{
			foreach (var method in owner.GetMethods(BindingFlags.Public | BindingFlags.Static)
				.Where(m => m.GetCustomAttribute<NorsePolicyAttribute>() is not null))
			{
				method.ReturnType.ShouldBe(typeof(void));
				method.GetParameters().Select(p => p.ParameterType)
					.ShouldBe([typeof(AuthorizationPolicyBuilder)]);
			}
		}
	}

	[Fact]
	void The_account_policy_is_declared_in_metadata() =>
		Should.NotThrow(() => Declaration(typeof(AuthNPolicies), AuthNPolicies.Account));

	[Fact]
	void The_account_policy_rejects_the_anonymous_role_principal() =>
		Evaluate(AuthNPolicies.Account, typeof(AuthNPolicies), Principal(NorseRoles.Anonymous)).Succeeded.ShouldBeFalse();

	[Fact]
	void The_account_policy_admits_an_authenticated_non_anonymous_principal() =>
		Evaluate(AuthNPolicies.Account, typeof(AuthNPolicies), Principal("user")).Succeeded.ShouldBeTrue();

	[Fact]
	void The_account_policy_rejects_an_unauthenticated_principal() =>
		Evaluate(AuthNPolicies.Account, typeof(AuthNPolicies), new ClaimsPrincipal(new ClaimsIdentity())).Succeeded.ShouldBeFalse();

	[Fact]
	void Self_now_rejects_the_anonymous_role_principal() =>
		Evaluate(IdentityPolicies.Self, typeof(IdentityPolicies), Principal(NorseRoles.Anonymous)).Succeeded.ShouldBeFalse();
}
