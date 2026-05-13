namespace Riverside.Elapsed.App.Models.Developer;

public sealed class OAuthGrantSummary
{
	public string GrantId { get; set; } = string.Empty;
	public string ServiceClientId { get; set; } = string.Empty;
	public string ServiceName { get; set; } = string.Empty;
	public IReadOnlyList<string> Scopes { get; set; } = Array.Empty<string>();
	public DateTimeOffset CreatedAt { get; set; }
	public DateTimeOffset? LastUsedAt { get; set; }
}
