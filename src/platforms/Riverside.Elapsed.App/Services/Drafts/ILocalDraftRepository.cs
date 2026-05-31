using Riverside.Elapsed.App.Models.Timelapses.Local;

namespace Riverside.Elapsed.App.Services.Drafts;

public interface ILocalDraftRepository
{
	Task<LocalDraftIndex> GetIndexAsync(CancellationToken ct = default);
	Task<LocalDraft?> GetDraftAsync(Guid localDraftId, CancellationToken ct = default);

	Task SaveDraftAsync(LocalDraft draft, CancellationToken ct = default);
	Task DeleteDraftAsync(Guid localDraftId, CancellationToken ct = default);
}
