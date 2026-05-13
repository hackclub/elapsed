namespace Riverside.Elapsed.App.Models.Developer;

public sealed class OAuthAppSummary
{
	public Guid AppId { get; set; }
	public string Name { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public Uri IconUrl { get; set; } = new Uri("https://example.com", UriKind.Absolute);
	public TrustLevel TrustLevel { get; set; }
	public IReadOnlyList<string> Scopes { get; set; } = Array.Empty<string>();
}
