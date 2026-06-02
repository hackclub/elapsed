#if false
namespace Riverside.Elapsed.App.Models.Timelapses.Local;

public sealed record LocalDraftIndexItem
{
	public Guid LocalDraftId { get; init; }
	public string Name { get; init; } = string.Empty;
	public DateTimeOffset LastModifiedAt { get; init; }

	public bool HasRemoteDraft { get; init; }
	public string? RemoteDraftTimelapseId { get; init; }
}

#endif
