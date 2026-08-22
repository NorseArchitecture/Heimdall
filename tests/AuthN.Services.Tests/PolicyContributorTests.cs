using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
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
}
