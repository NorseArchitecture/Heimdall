using Norse.Primitives;
using Norse.Primitives.Pii;

namespace Norse.AuthN.Services.Tests;

public sealed class RegisterRequestTests
{
	[Fact]
	void Object_initializer_assignment_hydrates_the_parse_state()
	{
		RegisterRequest request = new() { Email = "baw@example.com", Password = "p" };

		request.EmailParsed.TryGetValue(out Success<EmailAddress> success).ShouldBeTrue();
		success.Value.WireValue.ShouldBe("baw@example.com");
	}

	[Fact]
	void A_malformed_email_hydrates_a_failed_parse()
	{
		RegisterRequest request = new() { Email = "not-an-email", Password = "p" };

		request.EmailParsed.TryGetValue(out Success<EmailAddress> _).ShouldBeFalse();
	}

	[Fact]
	void Repeated_assignment_replaces_the_parse_state()
	{
		RegisterRequest request = new() { Email = "not-an-email", Password = "p" };

		request.Email = "fixed@example.com";

		request.EmailParsed.TryGetValue(out Success<EmailAddress> _).ShouldBeTrue();
	}

	[Fact]
	void Protobuf_round_trip_rehydrates_through_the_setter()
	{
		RegisterRequest original = new() { Email = "wire@example.com", Password = "p" };

		using MemoryStream stream = new();
		ProtoBuf.Serializer.Serialize(stream, original);
		stream.Position = 0;
		var roundTripped = ProtoBuf.Serializer.Deserialize<RegisterRequest>(stream);

		roundTripped.EmailParsed.TryGetValue(out Success<EmailAddress> success).ShouldBeTrue();
		success.Value.WireValue.ShouldBe("wire@example.com");
	}

	[Fact]
	void The_cached_state_always_equals_a_fresh_parse()
	{
		RegisterRequest request = new() { Email = "probe@example.com", Password = "p" };

		request.EmailParsed.ShouldBe(EmailAddress.Parse(request.Email));
	}
}
