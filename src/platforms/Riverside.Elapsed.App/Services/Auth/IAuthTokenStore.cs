using Riverside.Elapsed.App.Models.Auth;

namespace Riverside.Elapsed.App.Services.Auth;

public interface IAuthTokenStore
{
	string? AccessToken { get; }
	bool HasToken { get; }

	Task InitializeAsync(CancellationToken cancellationToken = default);
	Task SetTokenAsync(OAuthToken token, CancellationToken cancellationToken = default);
	Task ClearAsync(CancellationToken cancellationToken = default);
}
