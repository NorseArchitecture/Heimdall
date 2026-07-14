namespace Norse.AuthN.Components;

/// <summary>
/// The only thing any Razor component injects — never <see cref="IAuthenticationService"/> directly.
/// Two implementations exist, one per host: Yggdrasil's Hosting.Web.Server (Blazor Server, wraps the
/// mediator handlers directly, no wire) and Hosting.Web.Client (WASM, wraps the real gRPC-Web client
/// proxy). Both produce the same <see cref="AuthenticationResult"/> shape.
/// </summary>
public interface IAuthenticationGateway
{
	/// <summary>Authenticates a user with the provided credentials.</summary>
	/// <param name="request">The login request containing email and password.</param>
	/// <returns>A task representing the asynchronous login operation, containing the authentication result.</returns>
	Task<AuthenticationResult> Login(LoginRequest request);

	/// <summary>Registers a new user account with the provided credentials.</summary>
	/// <param name="request">The registration request containing email and password.</param>
	/// <returns>A task representing the asynchronous registration operation, containing the authentication result.</returns>
	Task<AuthenticationResult> Register(RegisterRequest request);

	/// <summary>Logs out the currently authenticated user.</summary>
	/// <param name="request">The logout request (the authenticated cookie identifies who is logging out).</param>
	/// <returns>A task representing the asynchronous logout operation, containing the authentication result.</returns>
	Task<AuthenticationResult> Logout(LogoutRequest request);
}
