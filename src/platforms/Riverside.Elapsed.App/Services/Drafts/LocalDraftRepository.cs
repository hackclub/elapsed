using Riverside.Elapsed.App.Models.Timelapses.Local;
using Riverside.Elapsed.App.Services.Storage;

namespace Riverside.Elapsed.App.Services.Drafts;

public sealed class LocalDraftRepository(ILocalJsonStore store) : ILocalDraftRepository
{
	private const string DraftsDir = "drafts";
	private static readonly string IndexPath = Path.Combine(DraftsDir, "index.json");

	private static string DraftPath(Guid id) => Path.Combine(DraftsDir, $"{id:D}.json");

	public async Task<LocalDraftIndex> GetIndexAsync(CancellationToken ct = default)
	{
		return await store.ReadAsync<LocalDraftIndex>(IndexPath, ct).ConfigureAwait(false)
			?? new LocalDraftIndex();
	}

	public Task<LocalDraft?> GetDraftAsync(Guid localDraftId, CancellationToken ct = default)
		=> store.ReadAsync<LocalDraft>(DraftPath(localDraftId), ct);

	public async Task SaveDraftAsync(LocalDraft draft, CancellationToken ct = default)
	{
		await store.WriteAsync(DraftPath(draft.LocalDraftId), draft, ct).ConfigureAwait(false); // persist draft contents

		var index = await GetIndexAsync(ct).ConfigureAwait(false);
		var newItem = new LocalDraftIndexItem
		{
			LocalDraftId = draft.LocalDraftId,
			Name = draft.Name,
			LastModifiedAt = draft.LastModifiedAt,
			HasRemoteDraft = draft.Remote is not null,
			RemoteDraftTimelapseId = draft.Remote?.DraftTimelapseId,
		};

		var updated = index.Drafts // replace existing & sort newest first
			.Where(x => x.LocalDraftId != draft.LocalDraftId)
			.Append(newItem)
			.OrderByDescending(x => x.LastModifiedAt)
			.ToArray();

		await store.WriteAsync(IndexPath, index with { Drafts = updated }, ct).ConfigureAwait(false);
	}

	public async Task DeleteDraftAsync(Guid localDraftId, CancellationToken ct = default)
	{
		await store.DeleteAsync(DraftPath(localDraftId), ct).ConfigureAwait(false);

		var index = await GetIndexAsync(ct).ConfigureAwait(false);
		var updated = index.Drafts.Where(x => x.LocalDraftId != localDraftId).ToArray();

		if (updated.Length == index.Drafts.Count)
			return;

		await store.WriteAsync(IndexPath, index with { Drafts = updated }, ct).ConfigureAwait(false);
	}
}
