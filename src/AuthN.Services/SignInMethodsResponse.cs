using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Norse.AuthN.Services;

/// <summary>
///     The wire response for <see cref="ISignInMethodsService.GetSignInMethods" /> — every link between the
///     sign-in pages, server-authored with the return URL already carried. Absent providers are <c>[]</c>,
///     never null.
/// </summary>
[DataContract]
public sealed record SignInMethodsResponse
{
	/// <summary>The URL of the password sign-in page.</summary>
	[DataMember(Order = 1)]
	public string PasswordSignInUrl { get; set; } = string.Empty;

	/// <summary>The URL of the passkey sign-in page.</summary>
	[DataMember(Order = 2)]
	public string PasskeySignInUrl { get; set; } = string.Empty;

	/// <summary>The configured external providers, each with its start URL.</summary>
	[DataMember(Order = 3)]
	[SuppressMessage("Design", "CA1819:Properties should not return arrays",
		Justification =
			"An init-only [DataContract] wire record: the array is the serialized repeated field, replaced whole on construction and never mutated in place — not a public collection-design surface.")]
	public ExternalProviderLink[] Providers { get; init; } = [];
}
