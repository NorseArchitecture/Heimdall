using System.ServiceModel;

namespace Norse.AuthN.Services;

/// <summary>
/// Issuance surface — real, network-callable gRPC methods that mint or clear the authenticated
/// cookie. No <c>CallContext</c> parameter, deliberately — see spec §9.2 for why this contract stays
/// off the full protobuf-net.Grpc package. Where the implementation needs a cancellation token or
/// <c>HttpContext</c>, it comes from a directly-injected <c>IHttpContextAccessor</c> instead.
/// </summary>
[ServiceContract(Name = "grpc.authentication.v1.AuthenticationService")]
public interface IAuthenticationService
{
	/// <summary>Authenticates a user with the provided credentials.</summary>
	/// <param name="request">The login request containing email and password.</param>
	/// <returns>A task representing the asynchronous login operation, containing the login result.</returns>
	[OperationContract]
	Task<LoginResult> Login(LoginRequest request);

	/// <summary>Registers a new user account with the provided credentials.</summary>
	/// <param name="request">The registration request containing email and password.</param>
	/// <returns>A task representing the asynchronous registration operation.</returns>
	[OperationContract]
	Task Register(RegisterRequest request);

	/// <summary>Logs out the currently authenticated user.</summary>
	/// <param name="request">The logout request (the authenticated cookie identifies who is logging out).</param>
	/// <returns>A task representing the asynchronous logout operation.</returns>
	[OperationContract]
	Task Logout(LogoutRequest request);
}
