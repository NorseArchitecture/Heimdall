using System.Runtime.Serialization;

namespace Norse.AuthN.Services;

/// <summary>One configured external provider the caller can still link (spec §4.3).</summary>
[DataContract]
public sealed record ExternalProviderEntry
{
	/// <summary>The provider's scheme name, passed to the link endpoint.</summary>
	[DataMember(Order = 1)]
	public required string Name { get; init; }

	/// <summary>The provider's human-readable name.</summary>
	[DataMember(Order = 2)]
	public required string DisplayName { get; init; }
}
