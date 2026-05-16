using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Kiota.Abstractions.Serialization;
using Microsoft.Kiota.Http.HttpClientLibrary;
using Riverside.Elapsed.App.Models.Auth;
using Riverside.Elapsed.App.Services.Api;
using Riverside.Elapsed.Auth.Token;
#if __WASM__
using Uno.Foundation;
#endif

namespace Riverside.Elapsed.App.Services.Auth;

public sealed class LapseAuthService(IAuthTokenStore tokenStore) : ILapseAuthService
{
	// match the cli's working flow exactly: the lapse server only accepts redirect_uris
	// that have been registered against the client_id, and port 8765 is the one that works.
	private const string DesktopRedirectUri = "http://localhost:8765/auth/callback";
	private const string DesktopListenerPrefix = "http://localhost:8765/";
#if __WASM__
	private const string PendingAuthStorageKey = "elapsed.auth.pending";
#endif

	public event EventHandler? LoggedOut;
	public event EventHandler? LoggedIn;

	public bool IsAuthenticated => tokenStore.HasToken;

	public async Task<bool> TryRestoreSessionAsync(CancellationToken cancellationToken = default)
	{
		await tokenStore.InitializeAsync(cancellationToken);
		if (tokenStore.HasToken)
		{
			return true;
		}

#if __WASM__
		var callbackResult = await TryCompleteWebCallbackAsync(cancellationToken);
		return callbackResult.IsSuccess && callbackResult.Value;
#else
		return false;
#endif
	}

	public async Task<ApiResult<bool>> LoginAsync(CancellationToken cancellationToken = default)
	{
#if __WASM__
		await BeginWebLoginAsync();
		return ApiResult<bool>.Failure(string.Empty);
#elif __ANDROID__ || __IOS__
		return ApiResult<bool>.Failure("Native mobile login is not implemented in v0.");
#else
		return await LoginDesktopAsync(cancellationToken);
#endif
	}

	public async Task LogoutAsync(CancellationToken cancellationToken = default)
	{
		await tokenStore.ClearAsync(cancellationToken);
		LoggedOut?.Invoke(this, EventArgs.Empty);
	}

	private static ApiClient CreateUnauthenticatedClient()
	{
		var adapter = new HttpClientRequestAdapter(new NoAuthProvider())
		{
			BaseUrl = Riverside.Elapsed.Constants.Endpoint,
		};
		return new ApiClient(adapter);
	}

	private async Task<ApiResult<bool>> LoginDesktopAsync(CancellationToken cancellationToken)
	{
		var (codeVerifier, codeChallenge) = GeneratePkceChallenge();
		var state = GenerateRandomString(32);
		var authorizeUrl = BuildAuthorizeUrl(DesktopRedirectUri, state, codeChallenge);

		if (!OpenBrowser(authorizeUrl))
		{
			return ApiResult<bool>.Failure("Could not open the browser for authentication.");
		}

		using var listener = new HttpListener();
		listener.Prefixes.Add(DesktopListenerPrefix);
		listener.Start();

		try
		{
			var callbackContext = await listener.GetContextAsync().WaitAsync(TimeSpan.FromMinutes(3), cancellationToken);
			var request = callbackContext.Request;
			var response = callbackContext.Response;

			var code = request.QueryString["code"];
			var returnedState = request.QueryString["state"];
			var error = request.QueryString["error"];

			if (!string.IsNullOrWhiteSpace(error))
			{
				await WriteListenerResponseAsync(response, 400, $"Authentication failed: {error}");
				return ApiResult<bool>.Failure($"Authentication failed: {error}");
			}

			if (string.IsNullOrWhiteSpace(code) || !string.Equals(state, returnedState, StringComparison.Ordinal))
			{
				await WriteListenerResponseAsync(response, 400, "Invalid callback received from the authentication server.");
				return ApiResult<bool>.Failure("Invalid callback received from the authentication server.");
			}

		var tokenResult = await ExchangeCodeForTokenAsync(code, codeVerifier, DesktopRedirectUri, cancellationToken);
		if (!tokenResult.IsSuccess || tokenResult.Value is null)
		{
			var message = tokenResult.ErrorMessage ?? "Authentication failed during token exchange.";
			await WriteListenerResponseAsync(response, 400, message);
			return ApiResult<bool>.Failure(message);
		}

			await tokenStore.SetTokenAsync(tokenResult.Value, cancellationToken);
			await WriteListenerResponseAsync(response, 200, "Authentication successful. You can close this tab now.");
			LoggedIn?.Invoke(this, EventArgs.Empty);
			return ApiResult<bool>.Success(true);
		}
		catch (TimeoutException)
		{
			return ApiResult<bool>.Failure("Authentication timed out before receiving a callback.");
		}
		finally
		{
			listener.Stop();
		}
	}

	private async Task<ApiResult<OAuthToken>> ExchangeCodeForTokenAsync(string code, string codeVerifier, string redirectUri, CancellationToken cancellationToken)
	{
		var client = CreateUnauthenticatedClient();
		var response = await client.Auth.Token.PostAsTokenPostResponseAsync(new TokenPostRequestBody
		{
			ClientId = Riverside.Elapsed.Constants.ClientId,
			Code = code,
			RedirectUri = redirectUri,
			CodeVerifier = codeVerifier,
			GrantType = new UntypedString("authorization_code"),
		}, cancellationToken: cancellationToken);

		if (string.IsNullOrWhiteSpace(response?.AccessToken))
		{
			return ApiResult<OAuthToken>.Failure("Token exchange failed: no access token returned by the server.");
		}

		var scope = string.IsNullOrWhiteSpace(response.Scope)
			? Riverside.Elapsed.Constants.OAuthScopes
			: response.Scope;
		var tokenType = response.TokenType switch
		{
			UntypedString untyped => string.IsNullOrWhiteSpace(untyped.GetValue()) ? "bearer" : untyped.GetValue()!,
			_ => "bearer",
		};

		return ApiResult<OAuthToken>.Success(new OAuthToken
		{
			AccessToken = response.AccessToken,
			RefreshToken = response.RefreshToken,
			ExpiresIn = response.ExpiresIn ?? 0,
			Scope = scope,
			TokenType = tokenType,
		});
	}

	private static async Task WriteListenerResponseAsync(HttpListenerResponse response, int statusCode, string body)
	{
		response.StatusCode = statusCode;
		var buffer = Encoding.UTF8.GetBytes(body);
		await response.OutputStream.WriteAsync(buffer);
		response.Close();
	}

	private static string BuildAuthorizeUrl(string redirectUri, string state, string codeChallenge)
	{
		return $"{Riverside.Elapsed.Constants.Endpoint}/auth/authorize" +
			$"?client_id={Uri.EscapeDataString(Riverside.Elapsed.Constants.ClientId)}" +
			$"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
			"&response_type=code" +
			$"&scope={Uri.EscapeDataString(Riverside.Elapsed.Constants.OAuthScopes)}" +
			$"&state={Uri.EscapeDataString(state)}" +
			$"&code_challenge={Uri.EscapeDataString(codeChallenge)}" +
			"&code_challenge_method=S256";
	}

	private static (string verifier, string challenge) GeneratePkceChallenge()
	{
		var verifier = GenerateRandomString(128);
		var challengeBytes = SHA256.HashData(Encoding.UTF8.GetBytes(verifier));
		var challenge = Convert.ToBase64String(challengeBytes)
			.Replace("+", "-", StringComparison.Ordinal)
			.Replace("/", "_", StringComparison.Ordinal)
			.TrimEnd('=');

		return (verifier, challenge);
	}

	private static string GenerateRandomString(int length)
	{
		const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
		var bytes = RandomNumberGenerator.GetBytes(length);
		var result = new char[length];
		for (var i = 0; i < length; i++)
		{
			result[i] = chars[bytes[i] % chars.Length];
		}

		return new string(result);
	}

	private static bool OpenBrowser(string url)
	{
		try
		{
			if (OperatingSystem.IsWindows())
			{
				System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
				{
					FileName = url,
					UseShellExecute = true,
				});
				return true;
			}

			if (OperatingSystem.IsMacOS())
			{
				System.Diagnostics.Process.Start("open", url);
				return true;
			}

			if (OperatingSystem.IsLinux())
			{
				System.Diagnostics.Process.Start("xdg-open", url);
				return true;
			}
		}
		catch (InvalidOperationException)
		{
		}
		catch (PlatformNotSupportedException)
		{
		}
		catch (System.ComponentModel.Win32Exception)
		{
		}

		return false;
	}

#if __WASM__
	private async Task BeginWebLoginAsync()
	{
		var (codeVerifier, codeChallenge) = GeneratePkceChallenge();
		var state = GenerateRandomString(32);
		var redirectUri = GetCurrentOrigin();

		var pending = JsonSerializer.Serialize(new PendingWebAuth
		{
			State = state,
			CodeVerifier = codeVerifier,
			RedirectUri = redirectUri,
		});

		WebAssemblyRuntime.InvokeJS($"globalThis.localStorage.setItem({JsonSerializer.Serialize(PendingAuthStorageKey)}, {JsonSerializer.Serialize(pending)});");

		var authorizeUrl = BuildAuthorizeUrl(redirectUri, state, codeChallenge);
		WebAssemblyRuntime.InvokeJS($"globalThis.location.assign({JsonSerializer.Serialize(authorizeUrl)});");
		await Task.CompletedTask;
	}

	private async Task<ApiResult<bool>> TryCompleteWebCallbackAsync(CancellationToken cancellationToken)
	{
		var href = WebAssemblyRuntime.InvokeJS("globalThis.location.href") ?? string.Empty;
		if (!Uri.TryCreate(href, UriKind.Absolute, out var callbackUri))
		{
			return ApiResult<bool>.Failure("Unable to parse browser callback URI.");
		}

		var query = ParseQuery(callbackUri.Query);
		if (!query.TryGetValue("code", out var code) || string.IsNullOrWhiteSpace(code))
		{
			return ApiResult<bool>.Failure("No authentication callback is pending.");
		}

		if (query.TryGetValue("error", out var error) && !string.IsNullOrWhiteSpace(error))
		{
			return ApiResult<bool>.Failure($"Authentication failed: {error}");
		}

		var pendingJson = WebAssemblyRuntime.InvokeJS($"globalThis.localStorage.getItem({JsonSerializer.Serialize(PendingAuthStorageKey)})") ?? string.Empty;
		if (string.IsNullOrWhiteSpace(pendingJson))
		{
			return ApiResult<bool>.Failure("Authentication callback is missing a pending PKCE session.");
		}

		var pending = JsonSerializer.Deserialize<PendingWebAuth>(pendingJson);
		if (pending is null || string.IsNullOrWhiteSpace(pending.State) || string.IsNullOrWhiteSpace(pending.CodeVerifier))
		{
			return ApiResult<bool>.Failure("Authentication callback has invalid PKCE state.");
		}

		if (!query.TryGetValue("state", out var returnedState) || !string.Equals(returnedState, pending.State, StringComparison.Ordinal))
		{
			return ApiResult<bool>.Failure("Authentication callback state did not match the pending request.");
		}

		var tokenResult = await ExchangeCodeForTokenAsync(code, pending.CodeVerifier, pending.RedirectUri, cancellationToken);
		if (!tokenResult.IsSuccess || tokenResult.Value is null)
		{
			return ApiResult<bool>.Failure(tokenResult.ErrorMessage);
		}

		await tokenStore.SetTokenAsync(tokenResult.Value, cancellationToken);
		WebAssemblyRuntime.InvokeJS($"globalThis.localStorage.removeItem({JsonSerializer.Serialize(PendingAuthStorageKey)});");
		WebAssemblyRuntime.InvokeJS("globalThis.history.replaceState({}, globalThis.document.title, globalThis.location.pathname);");
		LoggedIn?.Invoke(this, EventArgs.Empty);
		return ApiResult<bool>.Success(true);
	}

	private static string GetCurrentOrigin()
	{
		var origin = WebAssemblyRuntime.InvokeJS("globalThis.location.origin") ?? string.Empty;
		if (!string.IsNullOrWhiteSpace(origin))
		{
			return origin;
		}

		var href = WebAssemblyRuntime.InvokeJS("globalThis.location.href") ?? "https://elapsed.hackclub.com";
		return new Uri(href, UriKind.Absolute).GetLeftPart(UriPartial.Authority);
	}

	private static Dictionary<string, string> ParseQuery(string query)
	{
		var result = new Dictionary<string, string>(StringComparer.Ordinal);
		if (string.IsNullOrWhiteSpace(query))
		{
			return result;
		}

		var trimmed = query.TrimStart('?');
		foreach (var pair in trimmed.Split('&', StringSplitOptions.RemoveEmptyEntries))
		{
			var parts = pair.Split('=', 2);
			var key = Uri.UnescapeDataString(parts[0]);
			var value = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : string.Empty;
			result[key] = value;
		}

		return result;
	}

	private sealed class PendingWebAuth
	{
		public string State { get; set; } = string.Empty;
		public string CodeVerifier { get; set; } = string.Empty;
		public string RedirectUri { get; set; } = string.Empty;
	}
#endif
}
