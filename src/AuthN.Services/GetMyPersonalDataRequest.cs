using System.Runtime.Serialization;

namespace Norse.AuthN.Services;

/// <summary>
///     Deliberately empty — the wire request for <see cref="IIdentityService.GetMyPersonalDataAsync" />
///     carries no subject-id field at all, so asking about someone else through this method is
///     unrepresentable (spec §6.1). The caller's authenticated principal is the only subject this
///     method can ever disclose.
/// </summary>
[DataContract]
public sealed record GetMyPersonalDataRequest;
