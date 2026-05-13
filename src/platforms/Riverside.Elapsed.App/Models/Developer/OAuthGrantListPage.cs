namespace Riverside.Elapsed.App.Models.Developer;

public sealed class OAuthGrantListPage
{
	public IReadOnlyList<OAuthGrant> Grants { get; set; } = Array.Empty<OAuthGrant>();
}
