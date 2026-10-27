using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Norse.AuthN.Services;

namespace Norse.AuthN.Components.FluentUI.Tests.Manage;

public sealed class ManagePolicyLockTests
{
	// The Manage/_Imports.razor stamp is the only thing guarding these pages — a page moved out of the
	// folder, or a folder _Imports edit, silently drops the policy. This lock fails loud instead.
	[Fact]
	void Every_manage_route_carries_the_account_policy_and_the_manage_layout()
	{
		var pages = typeof(ManageLayout).Assembly.GetTypes()
			.Where(t => t.GetCustomAttributes<RouteAttribute>().Any(r => r.Template.StartsWith("/Account/Manage", StringComparison.OrdinalIgnoreCase)))
			.ToList();
		pages.Count.ShouldBeGreaterThanOrEqualTo(7);
		foreach (var page in pages)
		{
			page.GetCustomAttributes<AuthorizeAttribute>().ShouldContain(a => a.Policy == AuthNPolicies.Account, page.FullName);
			page.GetCustomAttribute<LayoutAttribute>()!.LayoutType.ShouldBe(typeof(ManageLayout), page.FullName);
		}
	}
}
