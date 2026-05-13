namespace Riverside.Elapsed.App.Models.Admin;

public sealed class AdminSearchResults
{
	public IReadOnlyList<AdminSearchResult> Results { get; set; } = Array.Empty<AdminSearchResult>();
}
