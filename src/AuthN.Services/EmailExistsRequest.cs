using System.Runtime.Serialization;
using Norse.Primitives;
using Norse.Primitives.Pii;

namespace Norse.AuthN.Services;

/// <summary>
///     The wire request for the pre-submit email-existence check — UX sugar over an inherently racy
///     lookup; the atomic user-creation conflict in the register handler remains the authority.
///     Stamped with no buffer: this is not a form-bound record — the sanctioned caller is
///     <c>RegisterRequestValidator</c>'s async rule, which chains after the email success gate and
///     passes the already-proven stamp through verbatim. A hostile caller gains nothing:
///     deserialization re-stamps, and the server-side validator converts a malformed or default
///     stamp to a failed outcome before the handler runs.
/// </summary>
[DataContract]
public sealed record EmailExistsRequest
{
	/// <summary>The email address to check, as a wire-stamped scalar.</summary>
	[DataMember(Order = 1)]
	public required Result<EmailAddress> Email { get; init; }
}
