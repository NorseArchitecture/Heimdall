using System.Runtime.Serialization;

namespace Norse.AuthN.Services;

/// <summary>
///     One configured external sign-in provider — an element of <see cref="SignInMethodsResponse.Providers" />.
/// </summary>
[DataContract]
public sealed record ExternalProviderLink
{
	/// <summary>The provider's human-readable name.</summary>
	[DataMember(Order = 1)]
	public string DisplayName { get; set; } = string.Empty;

	/// <summary>The server-authored URL that starts sign-in with this provider.</summary>
	[DataMember(Order = 2)]
	public string StartUrl { get; set; } = string.Empty;
}
