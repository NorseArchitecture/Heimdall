using System.Runtime.Serialization;

namespace Norse.AuthN.Services;

/// <summary>
///     The wire request for <see cref="ISignInMethodsService.GetSignInMethods" />. A pure wire DTO — no mediator marker, no <c>[Authorize]</c>.
/// </summary>
[DataContract]
public sealed record GetSignInMethodsRequest
{
	/// <summary>The return URL the sign-in page received, carried onto every link the server authors.</summary>
	[DataMember(Order = 1)]
	public string ReturnUrl { get; set; } = string.Empty;
}
