using System.Runtime.Serialization;

namespace Norse.AuthN.Services;

/// <summary>One external login linked to the caller's account (spec §4.3).</summary>
[DataContract]
public sealed record ExternalLoginEntry
{
	/// <summary>The provider's scheme name; with <see cref="ProviderKey" />, identifies the login to remove.</summary>
	[DataMember(Order = 1)]
	public required string LoginProvider { get; init; }

	/// <summary>The provider's opaque key for the user — never decoded by the client.</summary>
	[DataMember(Order = 2)]
	public required string ProviderKey { get; init; }

	/// <summary>The provider's human-readable name.</summary>
	[DataMember(Order = 3)]
	public required string DisplayName { get; init; }
}
