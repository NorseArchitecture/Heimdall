namespace Norse.AuthN.Services;

/// <summary>
/// Named authorization policies for the AuthN service surface. <see cref="Public"/> is satisfied by
/// any principal, anonymous-role cookie included — Login/Register/Logout must still declare a policy
/// per decided law item 4, even though that policy imposes no real requirement.
/// </summary>
public static class AuthNPolicies
{
	/// <summary>Satisfied by any authenticated-or-anonymous-cookie principal — no real requirement.</summary>
	public const string Public = "AuthN.Public";
}
