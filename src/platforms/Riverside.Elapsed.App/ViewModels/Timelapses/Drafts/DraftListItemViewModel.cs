namespace Riverside.Elapsed.App.ViewModels.Timelapses.Drafts;

public sealed partial class DraftListItemViewModel
{
	public Guid LocalDraftId { get; }
	public string Name { get; }
	public DateTimeOffset LastModifiedAt { get; }
	public bool HasRemoteDraft { get; }
	public string? RemoteDraftTimelapseId { get; }

	public DraftListItemViewModel(Guid localDraftId, string name, DateTimeOffset lastModifiedAt, bool hasRemoteDraft, string? remoteDraftTimelapseId)
	{
		LocalDraftId = localDraftId;
		Name = name;
		LastModifiedAt = lastModifiedAt;
		HasRemoteDraft = hasRemoteDraft;
		RemoteDraftTimelapseId = remoteDraftTimelapseId;
	}
}
