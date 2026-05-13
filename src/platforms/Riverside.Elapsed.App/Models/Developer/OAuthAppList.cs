namespace Riverside.Elapsed.App.Models.Developer;

public sealed class OAuthAppList
{
	public IReadOnlyList<DeveloperApp> Apps { get; set; } = Array.Empty<DeveloperApp>();
}
