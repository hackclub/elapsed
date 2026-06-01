#if false
namespace Riverside.Elapsed.App.Models.Developer;

public sealed class DeveloperApp
{
	public Guid AppId { get; set; }
	public string Name { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public Uri HomepageUrl { get; set; } = new Uri("https://example.com", UriKind.Absolute);
	public Uri? IconUrl { get; set; }
	public IReadOnlyList<Uri> RedirectUris { get; set; } = Array.Empty<Uri>();
	public IReadOnlyList<string> Scopes { get; set; } = Array.Empty<string>();
	public TrustLevel TrustLevel { get; set; }
	public string ClientId { get; set; } = string.Empty;
	public DateTimeOffset CreatedAt { get; set; }
	public User.User? CreatedBy { get; set; }
}

#endif
