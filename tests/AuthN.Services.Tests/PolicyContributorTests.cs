using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Norse.Abstractions.Components.Authorization;
using Norse.Infrastructure.Web.Server.Authentication;

namespace Norse.AuthN.Services.Tests;

public sealed class PolicyDeclarationTests
{
	// The registered JWT audience claim name, carried unchanged onto a Midgard-stamped bearer identity.
	const string AudienceClaimType = "aud";

	static MethodInfo Declaration(Type owner, string name) =>
		owner.GetMethods(BindingFlags.Public | BindingFlags.Static)
			.Single(m => m.GetCustomAttribute<NorsePolicyAttribute>()?.Name == name);

	static AuthorizationPolicy Build(Type owner, string name)
	{
		AuthorizationPolicyBuilder builder = new();
		Declaration(owner, name).Invoke(null, [builder]);
		return builder.Build();
	}

	// Registered the way Midgard's generated AddNorsePolicies() registers a declaration (configure, then the default
	// lane closure) and evaluated against the handlers a real host gets from AddNorseAuthentication().
	static AuthorizationResult Evaluate(string policyName, Type owner, ClaimsPrincipal principal)
	{
		ServiceCollection services = new();
		services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
		services.AddLogging();
		services.AddAuthorizationCore(options => options.AddPolicy(policyName, policy =>
		{
			Declaration(owner, policyName).Invoke(null, [policy]);
			policy.CloseLaneByDefault();
		}));
		services.AddNorseAuthentication();
		using var provider = services.BuildServiceProvider();
		return provider.GetRequiredService<IAuthorizationService>()
			.AuthorizeAsync(principal, null, policyName)
			.GetAwaiter()
			.GetResult();
	}

	// The shape Midgard's lane handlers hand to policy evaluation: one identity, typed by the lane that produced it.
	static ClaimsPrincipal Stamped(string lane, params Claim[] claims) =>
		new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "subject"), .. claims], lane));

	static ClaimsPrincipal Principal(string role) =>
		Stamped(NorseLanes.Identity, new Claim(ClaimTypes.Role, role));

	static ClaimsPrincipal UserBearer(string audience, params Claim[] claims) =>
		Stamped(NorseLanes.User, [new Claim(AudienceClaimType, audience), .. claims]);

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
	void The_account_policy_refuses_a_user_bearer_with_the_user_audience() =>
		Evaluate(AuthNPolicies.Account, typeof(AuthNPolicies), UserBearer(NorseAudiences.User)).Succeeded.ShouldBeFalse();

	[Fact]
	void Self_now_rejects_the_anonymous_role_principal() =>
		Evaluate(IdentityPolicies.Self, typeof(IdentityPolicies), Principal(NorseRoles.Anonymous)).Succeeded.ShouldBeFalse();

	[Fact]
	void The_account_read_policy_is_declared_in_metadata() =>
		Should.NotThrow(() => Declaration(typeof(AuthNPolicies), AuthNPolicies.AccountRead));

	[Fact]
	void Account_read_admits_a_cookie_identity() =>
		Evaluate(AuthNPolicies.AccountRead, typeof(AuthNPolicies), Principal("user")).Succeeded.ShouldBeTrue();

	[Fact]
	void Account_read_admits_a_user_bearer_with_the_user_audience() =>
		Evaluate(AuthNPolicies.AccountRead, typeof(AuthNPolicies), UserBearer(NorseAudiences.User)).Succeeded.ShouldBeTrue();

	[Fact]
	void Account_read_refuses_a_user_bearer_with_the_facade_audience() =>
		Evaluate(AuthNPolicies.AccountRead, typeof(AuthNPolicies), UserBearer(NorseAudiences.Facade)).Succeeded.ShouldBeFalse();

	[Fact]
	void Account_read_refuses_the_anonymous_identity() =>
		Evaluate(AuthNPolicies.AccountRead, typeof(AuthNPolicies), Principal(NorseRoles.Anonymous)).Succeeded.ShouldBeFalse();

	[Fact]
	void Account_read_refuses_a_machine_bearer_with_the_user_audience() =>
		Evaluate(AuthNPolicies.AccountRead, typeof(AuthNPolicies),
			Stamped(NorseLanes.Machine, new Claim(AudienceClaimType, NorseAudiences.User))).Succeeded.ShouldBeFalse();

	[Fact]
	void Account_read_refuses_an_unauthenticated_principal() =>
		Evaluate(AuthNPolicies.AccountRead, typeof(AuthNPolicies), new ClaimsPrincipal(new ClaimsIdentity())).Succeeded.ShouldBeFalse();

	[Fact]
	void Self_admits_a_cookie_identity() =>
		Evaluate(IdentityPolicies.Self, typeof(IdentityPolicies), Principal("user")).Succeeded.ShouldBeTrue();

	[Fact]
	void Self_admits_a_user_bearer_with_the_user_audience() =>
		Evaluate(IdentityPolicies.Self, typeof(IdentityPolicies), UserBearer(NorseAudiences.User)).Succeeded.ShouldBeTrue();

	[Fact]
	void Self_refuses_a_user_bearer_with_the_facade_audience() =>
		Evaluate(IdentityPolicies.Self, typeof(IdentityPolicies), UserBearer(NorseAudiences.Facade)).Succeeded.ShouldBeFalse();

	[Fact]
	void Self_refuses_a_machine_bearer_with_the_user_audience() =>
		Evaluate(IdentityPolicies.Self, typeof(IdentityPolicies),
			Stamped(NorseLanes.Machine, new Claim(AudienceClaimType, NorseAudiences.User))).Succeeded.ShouldBeFalse();

	[Fact]
	void Masked_disclosure_admits_a_system_role_cookie_identity() =>
		Evaluate(IdentityPolicies.MaskedDisclosure, typeof(IdentityPolicies), Principal(IdentityPolicies.SystemRole))
			.Succeeded.ShouldBeTrue();

	[Fact]
	void Masked_disclosure_refuses_a_system_role_on_the_user_lane() =>
		Evaluate(IdentityPolicies.MaskedDisclosure, typeof(IdentityPolicies),
			UserBearer(NorseAudiences.User, new Claim(ClaimTypes.Role, IdentityPolicies.SystemRole))).Succeeded.ShouldBeFalse();

	[Fact]
	void Masked_disclosure_names_its_lane_explicitly() =>
		Build(typeof(IdentityPolicies), IdentityPolicies.MaskedDisclosure).Requirements.OfType<LaneRequirement>()
			.ShouldHaveSingleItem().Lanes.ShouldBe([NorseLanes.Identity]);
}
