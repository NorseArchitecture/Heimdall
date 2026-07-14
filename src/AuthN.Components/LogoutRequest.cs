using System.Runtime.Serialization;

namespace Norse.AuthN.Components;

/// <summary>
/// Deliberately empty — the caller's authenticated cookie identifies who's logging out. A wire type
/// still exists per operation because protobuf-net.Grpc requires one.
/// </summary>
[DataContract]
public sealed record LogoutRequest;
