#if false
namespace Riverside.Elapsed.App.Models.Admin;

public sealed class AdminListResponse
{
	public EntityType Entity { get; set; }
	public IReadOnlyList<object> Rows { get; set; } = Array.Empty<object>();
	public long Total { get; set; }
	public long Page { get; set; }
	public long PageSize { get; set; }
}

#endif
