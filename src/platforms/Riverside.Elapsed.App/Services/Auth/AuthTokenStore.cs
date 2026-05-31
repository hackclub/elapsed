using Riverside.Elapsed.App.Models.Auth;
using Riverside.Elapsed.App.Services.Storage;

namespace Riverside.Elapsed.App.Services.Auth;

public sealed class AuthTokenStore(ILocalJsonStore store) : IAuthTokenStore
{
	private const string TokenPath = "auth\\oauth-token.json";
	private OAuthToken? _token;

	public string? AccessToken => _token?.AccessToken;

	public bool HasToken => !string.IsNullOrWhiteSpace(_token?.AccessToken);

	public async Task InitializeAsync(CancellationToken cancellationToken = default)
	{
		_token = await store.ReadAsync<OAuthToken>(TokenPath, cancellationToken);
	}

	public async Task SetTokenAsync(OAuthToken token, CancellationToken cancellationToken = default)
	{
		_token = token;
		await store.WriteAsync(TokenPath, token, cancellationToken);
	}

	public async Task ClearAsync(CancellationToken cancellationToken = default)
	{
		_token = null;
		await store.DeleteAsync(TokenPath, cancellationToken);
	}
}
