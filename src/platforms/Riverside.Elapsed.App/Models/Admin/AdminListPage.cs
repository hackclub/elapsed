using System.Text.Json;

namespace Riverside.Elapsed.App.Models.Admin;

public sealed class AdminListPage
{
	public EntityType Entity { get; set; }
	public IReadOnlyList<JsonElement> Rows { get; set; } = Array.Empty<JsonElement>();
	public long Total { get; set; }
	public long Page { get; set; }
	public long PageSize { get; set; }
}
