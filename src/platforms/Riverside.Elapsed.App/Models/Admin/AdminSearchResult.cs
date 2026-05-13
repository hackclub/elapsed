namespace Riverside.Elapsed.App.Models.Admin;

public sealed class AdminSearchResult
{
	public EntityType Entity { get; set; }
	public string Id { get; set; } = string.Empty;
	public string DisplayText { get; set; } = string.Empty;
}
