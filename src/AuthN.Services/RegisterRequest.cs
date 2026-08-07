using System.Runtime.Serialization;
using Norse.Primitives;
using Norse.Primitives.Pii;

namespace Norse.AuthN.Services;

/// <summary>Deliberately mutable — see <see cref="LoginRequest"/>'s remark. A pure wire DTO, same as <see cref="LoginRequest"/>.</summary>
[DataContract]
public sealed record RegisterRequest
{
	/// <summary>The email address as entered. Assignment hydrates <see cref="EmailParsed"/> — there is no code path that sets the string without refreshing the parse state.</summary>
	[DataMember(Order = 1)]
	public required string Email
	{
		get;
		set
		{
			field = value;
			EmailParsed = EmailAddress.Parse(value);
		}
	} = "";

	/// <summary>The cached parse of <see cref="Email"/> — never serialized; deserialization hydrates it by construction because protobuf-net assigns through the same setter.</summary>
	public Result<EmailAddress> EmailParsed { get; private set; } = EmailAddress.Parse("");

	/// <summary>The password for the new account.</summary>
	[DataMember(Order = 2)]
	public required string Password { get; set; }
}
