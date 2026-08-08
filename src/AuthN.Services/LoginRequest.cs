using System.Runtime.Serialization;
using Norse.Primitives;
using Norse.Primitives.Pii;

namespace Norse.AuthN.Services;

/// <summary>
///     Deliberately mutable (not <c>init</c>) — this is the direct two-way <c>EditForm</c> binding
///     target for <c>AuthN.Components.FluentUI</c>'s <c>Login.razor</c>. A pure wire DTO — no
///     mediator marker, no <c>[Authorize]</c>. Himinbjörg's server-sovereign <c>LoginCommand</c>
///     wraps this instance to give it mediator identity; the wire shape itself stays free of
///     anything that would drag <c>Abstractions.Web.Server</c> into WASM's footprint.
/// </summary>
[DataContract]
public sealed record LoginRequest
{
	/// <summary>
	///     The email address as a wire-stamped scalar — the serialized member. Non-nullable, so the
	///     field is required: the forge mints the verdict, the request declares the obligation
	///     (spec 2026-08-08-wire-stamped-request-scalars). Deserialization is the parse event; the
	///     server holds its own verdict regardless of what the client claimed.
	/// </summary>
	[DataMember(Order = 1)]
	public Result<EmailAddress> Email { get; set; }

	/// <summary>
	///     The form's raw buffer — never serialized; assignment stamps <see cref="Email" />, so
	///     there is no code path that sets the text without refreshing the verdict. A client-side
	///     artifact: the sanctioned deserialization path never assigns it, so on the server it
	///     legitimately holds its empty default.
	/// </summary>
	public required string EmailInput
	{
		get;
		set
		{
			field = value;
			Email = EmailAddress.Parse(value);
		}
	} = string.Empty;

	/// <summary>The user's password.</summary>
	[DataMember(Order = 2)]
	public string Password { get; set; } = string.Empty;

	/// <summary>Whether to persist the authentication cookie across browser sessions.</summary>
	[DataMember(Order = 3)]
	public bool RememberMe { get; set; }
}
