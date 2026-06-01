using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Riverside.Elapsed.App.Services.Upload;

public sealed class LapseService : IDisposable
{
	private const string BaseUrl = Riverside.Elapsed.Constants.Endpoint;
	private const string UploadUrl = "https://api.lapse.hackclub.com/upload";
	private const string DraftEditorBase = "https://lapse.hackclub.com/draft";
	private const string ClientId = Riverside.Elapsed.Constants.ClientId;
	private const string OAuthScopes = Riverside.Elapsed.Constants.OAuthScopes;
	private const string RedirectUri = "http://localhost:8765/auth/callback";
	private const int ChunkSize = 4 * 1024 * 1024;

	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
	};

	private const string WebPortalBase = "https://lapse.hackclub.com";

	private readonly HttpClient _http = new();

	private StoredAuth? _auth;
	private StoredDevice? _device;

	private static string DataDir => Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
		"Riverside", "Elapsed");

	private static string AuthPath => Path.Combine(DataDir, "auth.json");
	private static string DevicePath => Path.Combine(DataDir, "device.json");

	public bool IsAuthenticated => _auth is not null;

	public async Task InitializeAsync(CancellationToken ct = default)
	{
		_auth = await LoadJsonAsync<StoredAuth>(AuthPath, ct);
		_device = await LoadJsonAsync<StoredDevice>(DevicePath, ct);
	}

	public async Task SignInAsync(CancellationToken ct = default)
	{
		if (_auth is not null)
			return;

		_auth = await RunOAuthPkceAsync(ct);
		await SaveJsonAsync(AuthPath, _auth, ct);
	}

	public Task SignOutAsync(CancellationToken ct = default)
	{
		_auth = null;
		if (File.Exists(AuthPath))
			File.Delete(AuthPath);
		return Task.CompletedTask;
	}

	public async Task<UserProfile?> GetCurrentUserAsync(CancellationToken ct = default)
	{
		if (_auth is null)
			return null;

		using var req = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/user/myself");
		req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _auth.AccessToken);

		using var res = await _http.SendAsync(req, ct);
		if (!res.IsSuccessStatusCode)
			return null;

		var json = await res.Content.ReadAsStringAsync(ct);
		using var doc = JsonDocument.Parse(json);

		if (!doc.RootElement.TryGetProperty("data", out var data))
			return null;

		var user = data.GetProperty("user");
		if (user.ValueKind == JsonValueKind.Null)
			return null;

		return new UserProfile(
			user.GetProperty("id").GetString()!,
			user.GetProperty("handle").GetString()!,
			user.GetProperty("displayName").GetString()!,
			user.TryGetProperty("profilePictureUrl", out var pfp) ? pfp.GetString() : null);
	}

	public static void OpenProfileInBrowser(string handle)
	{
		OpenUrl($"{WebPortalBase}/user/@{handle}");
	}

	public async Task<string> UploadDraftAsync(
		string sessionFilePath,
		byte[] thumbnailBytes,
		TimeSpan duration,
		IProgress<UploadProgress>? progress = null,
		CancellationToken ct = default)
	{
		progress?.Report(new(UploadPhase.Authenticating, 0, "Signing in..."));
		await EnsureAuthenticatedAsync(ct);

		progress?.Report(new(UploadPhase.Authenticating, 0.5, "Registering device..."));
		await EnsureDeviceRegisteredAsync(ct);

		var sessionBytes = await File.ReadAllBytesAsync(sessionFilePath, ct);
		var key = Convert.FromHexString(_device!.PasskeyHex);

		long encryptedSessionSize = ComputeEncryptedSize(sessionBytes.Length);
		long encryptedThumbnailSize = thumbnailBytes.Length > 0
			? ComputeEncryptedSize(thumbnailBytes.Length)
			: ComputeEncryptedSize(1);

		if (thumbnailBytes.Length == 0)
			thumbnailBytes = [0];

		var snapshots = GenerateSnapshots(duration);

		progress?.Report(new(UploadPhase.CreatingDraft, 0, "Creating draft..."));
		var draft = await CreateDraftAsync(
			"Elapsed Recording",
			snapshots,
			_device.DeviceId,
			encryptedSessionSize,
			encryptedThumbnailSize,
			ct);

		var iv = Convert.FromHexString(draft.Iv);

		progress?.Report(new(UploadPhase.Encrypting, 0, "Encrypting session..."));
		var encryptedSession = EncryptAesCbc(sessionBytes, key, iv);

		progress?.Report(new(UploadPhase.Encrypting, 0.5, "Encrypting thumbnail..."));
		var encryptedThumbnail = EncryptAesCbc(thumbnailBytes, key, iv);

		progress?.Report(new(UploadPhase.UploadingSession, 0, "Uploading session..."));
		await TusUploadAsync(encryptedSession, draft.SessionUploadToken, p =>
			progress?.Report(new(UploadPhase.UploadingSession, p, "Uploading session...")), ct);

		progress?.Report(new(UploadPhase.UploadingThumbnail, 0, "Uploading thumbnail..."));
		await TusUploadAsync(encryptedThumbnail, draft.ThumbnailUploadToken, p =>
			progress?.Report(new(UploadPhase.UploadingThumbnail, p, "Uploading thumbnail...")), ct);

		progress?.Report(new(UploadPhase.Complete, 1, "Done!"));
		return draft.DraftId;
	}

	public async Task UpdateDraftAsync(
		string draftId, string name, string? description,
		CancellationToken ct = default)
	{
		using var req = new HttpRequestMessage(new HttpMethod("PATCH"), $"{BaseUrl}/draftTimelapse/update");
		req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _auth!.AccessToken);

		var changes = new Dictionary<string, object>
		{
			["name"] = name,
			["editList"] = (object[])[],
		};
		if (description is not null)
			changes["description"] = description;

		req.Content = new StringContent(
			JsonSerializer.Serialize(new { id = draftId, changes }, JsonOptions),
			Encoding.UTF8, "application/json");

		using var res = await _http.SendAsync(req, ct);
		if (!res.IsSuccessStatusCode)
		{
			var body = await res.Content.ReadAsStringAsync(ct);
			throw new HttpRequestException($"Draft update failed ({res.StatusCode}): {body}");
		}
	}

	public async Task<string> PublishDraftAsync(
		string draftId, string visibility,
		CancellationToken ct = default)
	{
		using var req = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/timelapse/publish");
		req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _auth!.AccessToken);
		req.Content = new StringContent(
			JsonSerializer.Serialize(new
			{
				id = draftId,
				visibility,
				deviceKey = _device!.PasskeyHex,
			}, JsonOptions),
			Encoding.UTF8, "application/json");

		using var res = await _http.SendAsync(req, ct);
		var body = await res.Content.ReadAsStringAsync(ct);

		if (!res.IsSuccessStatusCode)
			throw new HttpRequestException($"Publish failed ({res.StatusCode}): {body}");

		using var doc = JsonDocument.Parse(body);
		return doc.RootElement
			.GetProperty("data")
			.GetProperty("timelapse")
			.GetProperty("id")
			.GetString()!;
	}

	public static void OpenTimelapseInBrowser(string timelapseId)
		=> OpenUrl($"{WebPortalBase}/timelapse/{timelapseId}");

	public static void OpenDraftInBrowser(string draftId)
		=> OpenUrl($"{DraftEditorBase}/{draftId}");

	private static void OpenUrl(string url)
	{
		try
		{
			Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
		}
		catch (Exception) { }
	}

	private async Task EnsureAuthenticatedAsync(CancellationToken ct)
	{
		if (_auth is not null)
			return;

		_auth = await LoadJsonAsync<StoredAuth>(AuthPath, ct);
		if (_auth is not null)
			return;

		_auth = await RunOAuthPkceAsync(ct);
		await SaveJsonAsync(AuthPath, _auth, ct);
	}

	private async Task EnsureDeviceRegisteredAsync(CancellationToken ct)
	{
		if (_device is not null)
			return;

		_device = await LoadJsonAsync<StoredDevice>(DevicePath, ct);
		if (_device is not null)
			return;

		var deviceName = Environment.MachineName;
		using var req = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/user/registerDevice");
		req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _auth!.AccessToken);
		req.Content = new StringContent(
			JsonSerializer.Serialize(new { name = deviceName }, JsonOptions),
			Encoding.UTF8, "application/json");

		using var res = await _http.SendAsync(req, ct);
		res.EnsureSuccessStatusCode();

		var json = await res.Content.ReadAsStringAsync(ct);
		using var doc = JsonDocument.Parse(json);
		var deviceId = doc.RootElement.GetProperty("data").GetProperty("device").GetProperty("id").GetString()!;

		var passkey = new byte[16];
		RandomNumberGenerator.Fill(passkey);

		_device = new StoredDevice(deviceId, Convert.ToHexString(passkey).ToLowerInvariant());
		await SaveJsonAsync(DevicePath, _device, ct);
	}

	private async Task<StoredAuth> RunOAuthPkceAsync(CancellationToken ct)
	{
		var (codeVerifier, codeChallenge) = GeneratePkceChallenge();
		var state = GenerateRandomString(32);

		var authorizeUrl = $"{BaseUrl}/auth/authorize" +
			$"?client_id={Uri.EscapeDataString(ClientId)}" +
			$"&redirect_uri={Uri.EscapeDataString(RedirectUri)}" +
			$"&response_type=code" +
			$"&scope={Uri.EscapeDataString(OAuthScopes)}" +
			$"&state={Uri.EscapeDataString(state)}" +
			$"&code_challenge={Uri.EscapeDataString(codeChallenge)}" +
			$"&code_challenge_method=S256";

		try
		{
			Process.Start(new ProcessStartInfo { FileName = authorizeUrl, UseShellExecute = true });
		}
		catch (Exception) { }

		using var listener = new HttpListener();
		listener.Prefixes.Add("http://localhost:8765/");
		listener.Start();

		try
		{
			var context = await listener.GetContextAsync().WaitAsync(ct);
			var code = context.Request.QueryString["code"];
			var returnedState = context.Request.QueryString["state"];
			var error = context.Request.QueryString["error"];

			if (!string.IsNullOrEmpty(error) || string.IsNullOrEmpty(code) || returnedState != state)
			{
				SendListenerResponse(context.Response, 400, error ?? "Authentication failed");
				throw new InvalidOperationException($"OAuth failed: {error ?? "invalid response"}");
			}

			SendListenerResponse(context.Response, 200, "Authentication successful! You can close this window.");

			return await ExchangeCodeForTokenAsync(code, codeVerifier, ct);
		}
		finally
		{
			listener.Stop();
		}
	}

	private async Task<StoredAuth> ExchangeCodeForTokenAsync(string code, string codeVerifier, CancellationToken ct)
	{
		using var req = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/auth/token");
		req.Content = new StringContent(
			JsonSerializer.Serialize(new
			{
				grant_type = "authorization_code",
				code,
				redirect_uri = RedirectUri,
				client_id = ClientId,
				code_verifier = codeVerifier,
			}, JsonOptions),
			Encoding.UTF8, "application/json");

		using var res = await _http.SendAsync(req, ct);
		res.EnsureSuccessStatusCode();

		var json = await res.Content.ReadAsStringAsync(ct);
		using var doc = JsonDocument.Parse(json);
		var root = doc.RootElement;

		return new StoredAuth(
			root.GetProperty("access_token").GetString()!,
			root.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null);
	}

	private async Task<DraftCreateResult> CreateDraftAsync(
		string name, long[] snapshots, string deviceId,
		long encryptedSessionSize, long encryptedThumbnailSize,
		CancellationToken ct)
	{
		using var req = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/draftTimelapse/create");
		req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _auth!.AccessToken);
		req.Content = new StringContent(
			JsonSerializer.Serialize(new
			{
				name,
				snapshots,
				deviceId,
				sessions = new[] { new { fileSize = encryptedSessionSize } },
				thumbnailSize = encryptedThumbnailSize,
			}, JsonOptions),
			Encoding.UTF8, "application/json");

		using var res = await _http.SendAsync(req, ct);
		var body = await res.Content.ReadAsStringAsync(ct);

		if (!res.IsSuccessStatusCode)
			throw new HttpRequestException($"Draft creation failed ({res.StatusCode}): {body}");

		using var doc = JsonDocument.Parse(body);
		var data = doc.RootElement.GetProperty("data");
		var draft = data.GetProperty("draftTimelapse");
		var tokens = data.GetProperty("sessionUploadTokens");

		return new DraftCreateResult(
			draft.GetProperty("id").GetString()!,
			draft.GetProperty("iv").GetString()!,
			tokens[0].GetString()!,
			data.GetProperty("thumbnailUploadToken").GetString()!);
	}

	private async Task TusUploadAsync(byte[] data, string uploadToken, Action<double>? progress, CancellationToken ct)
	{
		using var createReq = new HttpRequestMessage(HttpMethod.Post, UploadUrl);
		createReq.Headers.TryAddWithoutValidation("Tus-Resumable", "1.0.0");
		createReq.Headers.TryAddWithoutValidation("Upload-Length", data.Length.ToString());
		createReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", uploadToken);

		using var createRes = await _http.SendAsync(createReq, ct);
		var createBody = await createRes.Content.ReadAsStringAsync(ct);

		if (!createRes.IsSuccessStatusCode)
			throw new HttpRequestException($"TUS create failed ({createRes.StatusCode}): {createBody}");

		var location = createRes.Headers.Location
			?? throw new InvalidOperationException("TUS create response missing Location header");

		long offset = 0;
		while (offset < data.Length)
		{
			var chunkSize = (int)Math.Min(ChunkSize, data.Length - offset);

			using var patchReq = new HttpRequestMessage(new HttpMethod("PATCH"), location);
			patchReq.Headers.TryAddWithoutValidation("Tus-Resumable", "1.0.0");
			patchReq.Headers.TryAddWithoutValidation("Upload-Offset", offset.ToString());
			patchReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", uploadToken);
			patchReq.Content = new ByteArrayContent(data, (int)offset, chunkSize);
			patchReq.Content.Headers.ContentType = new MediaTypeHeaderValue("application/offset+octet-stream");

			using var patchRes = await _http.SendAsync(patchReq, ct);
			if (!patchRes.IsSuccessStatusCode)
			{
				var patchBody = await patchRes.Content.ReadAsStringAsync(ct);
				throw new HttpRequestException($"TUS upload failed ({patchRes.StatusCode}): {patchBody}");
			}

			if (patchRes.Headers.TryGetValues("Upload-Offset", out var offsets))
				offset = long.Parse(offsets.First());
			else
				offset += chunkSize;

			progress?.Invoke((double)offset / data.Length);
		}
	}

	private static byte[] EncryptAesCbc(byte[] data, byte[] key, byte[] iv)
	{
		using var aes = Aes.Create();
		aes.Mode = CipherMode.CBC;
		aes.Padding = PaddingMode.PKCS7;
		aes.Key = key;
		aes.IV = iv;

		using var encryptor = aes.CreateEncryptor();
		return encryptor.TransformFinalBlock(data, 0, data.Length);
	}

	private static long ComputeEncryptedSize(long plainSize)
		=> ((plainSize / 16) + 1) * 16;

	private static long[] GenerateSnapshots(TimeSpan duration)
	{
		var now = DateTimeOffset.UtcNow;
		var start = now - duration;
		var count = Math.Max(1, (int)duration.TotalSeconds);
		var snapshots = new long[count];
		for (int i = 0; i < count; i++)
			snapshots[i] = start.AddSeconds(i).ToUnixTimeMilliseconds();
		return snapshots;
	}

	private static (string verifier, string challenge) GeneratePkceChallenge()
	{
		var verifier = GenerateRandomString(128);
		var challengeBytes = SHA256.HashData(Encoding.UTF8.GetBytes(verifier));
		var challenge = Convert.ToBase64String(challengeBytes)
			.Replace("+", "-")
			.Replace("/", "_")
			.TrimEnd('=');
		return (verifier, challenge);
	}

	private static string GenerateRandomString(int length)
	{
		const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
		var buf = new byte[length];
		RandomNumberGenerator.Fill(buf);
		var result = new char[length];
		for (int i = 0; i < length; i++)
			result[i] = chars[buf[i] % chars.Length];
		return new string(result);
	}

	private static void SendListenerResponse(HttpListenerResponse response, int statusCode, string body)
	{
		response.StatusCode = statusCode;
		var buffer = Encoding.UTF8.GetBytes(body);
		response.OutputStream.Write(buffer, 0, buffer.Length);
		response.Close();
	}

	private static async Task<T?> LoadJsonAsync<T>(string path, CancellationToken ct) where T : class
	{
		if (!File.Exists(path))
			return null;

		var json = await File.ReadAllTextAsync(path, ct);
		return JsonSerializer.Deserialize<T>(json, JsonOptions);
	}

	private static async Task SaveJsonAsync<T>(string path, T value, CancellationToken ct)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		var json = JsonSerializer.Serialize(value, JsonOptions);
		await File.WriteAllTextAsync(path, json, ct);
	}

	public void Dispose() => _http.Dispose();

	private sealed record StoredAuth(
		[property: JsonPropertyName("accessToken")] string AccessToken,
		[property: JsonPropertyName("refreshToken")] string? RefreshToken);

	private sealed record StoredDevice(
		[property: JsonPropertyName("deviceId")] string DeviceId,
		[property: JsonPropertyName("passkeyHex")] string PasskeyHex);

	private sealed record DraftCreateResult(
		string DraftId, string Iv, string SessionUploadToken, string ThumbnailUploadToken);
}

public sealed partial record UserProfile(string Id, string Handle, string DisplayName, string? ProfilePictureUrl);

public sealed record UploadProgress(UploadPhase Phase, double Fraction, string Description);

public enum UploadPhase
{
	Authenticating,
	CreatingDraft,
	Encrypting,
	UploadingSession,
	UploadingThumbnail,
	Publishing,
	Complete,
}
