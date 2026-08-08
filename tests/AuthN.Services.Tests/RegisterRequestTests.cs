using Norse.Primitives;
using Norse.Primitives.Pii;

namespace Norse.AuthN.Services.Tests;

public sealed class RegisterRequestTests
{
	[Fact]
	void Object_initializer_assignment_stamps_the_email()
	{
		RegisterRequest request = new() { EmailInput = "baw@example.com", Password = "p" };

		request.Email.TryGetValue(out Success<EmailAddress> success).ShouldBeTrue();
		success.Value.WireValue.ShouldBe("baw@example.com");
	}

	[Fact]
	void A_malformed_email_stamps_a_failed_parse()
	{
		RegisterRequest request = new() { EmailInput = "not-an-email", Password = "p" };

		request.Email.TryGetValue(out Success<EmailAddress> _).ShouldBeFalse();
	}

	[Fact]
	void Repeated_assignment_replaces_the_stamp()
	{
		RegisterRequest request = new() { EmailInput = "not-an-email", Password = "p" };

		request.EmailInput = "fixed@example.com";

		request.Email.TryGetValue(out Success<EmailAddress> _).ShouldBeTrue();
	}

	// The wire round trip of a stamped member is Midgard's proof (PiiResultSerializerTests — the
	// wire law lives there, registered by the generated wiring; this realm legally cannot reference
	// it). What this realm owns is the serialization CONTRACT: the stamp is the [DataMember], the
	// buffer is not on the wire at all.
	[Fact]
	void The_stamp_is_the_serialized_member_and_the_buffer_is_not()
	{
		typeof(RegisterRequest).GetProperty(nameof(RegisterRequest.Email))!
			.IsDefined(typeof(System.Runtime.Serialization.DataMemberAttribute), inherit: false).ShouldBeTrue();
		typeof(RegisterRequest).GetProperty(nameof(RegisterRequest.EmailInput))!
			.IsDefined(typeof(System.Runtime.Serialization.DataMemberAttribute), inherit: false).ShouldBeFalse();
	}

	[Fact]
	void The_stamp_always_equals_a_fresh_parse()
	{
		RegisterRequest request = new() { EmailInput = "probe@example.com", Password = "p" };

		request.Email.ShouldBe(EmailAddress.Parse(request.EmailInput));
	}
}
