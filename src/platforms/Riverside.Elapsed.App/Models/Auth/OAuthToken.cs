#if false
namespace Riverside.Elapsed.App.Models.Auth;

public sealed class OAuthToken
{
	public string AccessToken { get; set; } = string.Empty;
	public string TokenType { get; set; } = string.Empty;
	public double ExpiresIn { get; set; }
	public string Scope { get; set; } = string.Empty;
	public string? RefreshToken { get; set; }
}

#endif
