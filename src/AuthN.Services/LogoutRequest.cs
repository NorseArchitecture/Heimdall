using System.Runtime.Serialization;
using Microsoft.AspNetCore.Authorization;
using Norse.Abstractions.Contracts;

namespace Norse.AuthN.Services;

/// <summary>
/// Deliberately empty — the caller's authenticated cookie identifies who's logging out. A wire type
/// still exists per operation because protobuf-net.Grpc requires one. Marked as a command request
/// alongside <see cref="LoginRequest"/> and <see cref="RegisterRequest"/>.
/// </summary>
[DataContract]
[Authorize(Policy = AuthNPolicies.Public)]
public sealed record LogoutRequest : ICommandRequest<Unit>;
