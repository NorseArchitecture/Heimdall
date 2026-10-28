namespace Norse.AuthN.Components.FluentUI;

/// <summary>
///     The answer of one browser WebAuthn ceremony through <see cref="PasskeyModule" />, the shape
///     <c>wwwroot/passkeys.js</c> returns (<c>{ credentialJson, error }</c>). Exactly one of three states:
///     a credential (<see cref="CredentialJson" /> set) to hand the server; an error (<see cref="Error" /> set)
///     to render in place, a dismissed prompt included; or neither, an aborted ceremony, which renders
///     nothing and calls nothing.
/// </summary>
/// <param name="CredentialJson">The browser's credential, as JSON; null unless the ceremony completed.</param>
/// <param name="Error">The line to render; null unless the ceremony failed.</param>
sealed record PasskeyCeremonyResult(string? CredentialJson, string? Error);
