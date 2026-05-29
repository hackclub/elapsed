using Riverside.Elapsed.App.Services.Api;

namespace Riverside.Elapsed.App.Services.Auth;

public interface ILapseAuthService
{
	event EventHandler? LoggedOut;
	event EventHandler? LoggedIn;

	/// <summary>Gets a value indicating whether a valid session token is currently stored.</summary>
	bool IsAuthenticated { get; }

	Task<bool> TryRestoreSessionAsync(CancellationToken cancellationToken = default);
	Task<ApiResult<bool>> LoginAsync(CancellationToken cancellationToken = default);
	Task LogoutAsync(CancellationToken cancellationToken = default);
}
