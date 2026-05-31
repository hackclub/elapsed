using System.Collections.ObjectModel;
using Riverside.Elapsed.App.Models.Timelapses.Local;
using Riverside.Elapsed.App.Services.Drafts;

namespace Riverside.Elapsed.App.ViewModels.Timelapses.Drafts;

public sealed partial class DraftsViewModel : ObservableObject
{
	private readonly ILocalDraftRepository _drafts;
	private readonly INavigator _navigator;
	private readonly IDispatcher _dispatcher;

	[ObservableProperty]
	private bool _isLoading;

	[ObservableProperty]
	private string? _errorMessage;

	public ObservableCollection<DraftListItemViewModel> Items { get; }

	public IAsyncRelayCommand RefreshCommand { get; }
	public IAsyncRelayCommand CreateNewDraftCommand { get; }
	public IAsyncRelayCommand<Guid> DeleteDraftCommand { get; }
	public IAsyncRelayCommand<Guid> OpenDraftCommand { get; }

	public DraftsViewModel(ILocalDraftRepository drafts, INavigator navigator, IDispatcher dispatcher)
	{
		_drafts = drafts;
		_navigator = navigator;
		_dispatcher = dispatcher;

		Items = [];

		RefreshCommand = new AsyncRelayCommand(RefreshAsync);
		CreateNewDraftCommand = new AsyncRelayCommand(CreateNewDraftAsync);
		DeleteDraftCommand = new AsyncRelayCommand<Guid>(DeleteDraftAsync);
		OpenDraftCommand = new AsyncRelayCommand<Guid>(OpenDraftAsync);
	}

	public async Task RefreshAsync()
	{
		try
		{
			ErrorMessage = null;
			IsLoading = true;

			var index = await _drafts.GetIndexAsync();

			await _dispatcher.ExecuteAsync(() =>
			{
				Items.Clear();
				foreach (var d in index.Drafts)
				{
					Items.Add(new(
						d.LocalDraftId,
						string.IsNullOrWhiteSpace(d.Name) ? "Untitled draft" : d.Name, // TODO: Localise
						d.LastModifiedAt,
						d.HasRemoteDraft,
						d.RemoteDraftTimelapseId));
				}
			});
		}
		catch (Exception ex)
		{
			ErrorMessage = ex.Message;
		}
		finally
		{
			IsLoading = false;
		}
	}

	public async Task CreateNewDraftAsync()
	{
		try
		{
			ErrorMessage = null;

			var deviceId = Guid.Empty; // TODO: Replace with local device registration store
			var now = DateTimeOffset.UtcNow;
			var draft = new LocalDraft
			{
				LocalDraftId = Guid.NewGuid(),
				CreatedAt = now,
				LastModifiedAt = now,
				Name = string.Empty,
				Description = string.Empty,
				Snapshots = [],
				EditList = [],
				Sessions = [],
				Thumbnail = new(),
				Remote = null,
				State = new(),
			};

			await _drafts.SaveDraftAsync(draft);
			await RefreshAsync();
			await OpenDraftAsync(draft.LocalDraftId);
		}
		catch (Exception ex)
		{
			ErrorMessage = ex.Message;
		}
	}

	private async Task DeleteDraftAsync(Guid localDraftId)
	{
		try
		{
			ErrorMessage = null;

			await _drafts.DeleteDraftAsync(localDraftId);
			await _dispatcher.ExecuteAsync(() =>
			{
				var item = Items.FirstOrDefault(x => x.LocalDraftId == localDraftId);
				if (item is not null)
				{
					Items.Remove(item);
				}
			});
		}
		catch (Exception ex)
		{
			ErrorMessage = ex.Message;
		}
	}

	private Task OpenDraftAsync(Guid localDraftId)
	{
		return _navigator.NavigateViewModelAsync<DraftDetailsViewModel>(
			this,
			data: new DraftListItem(localDraftId));
	}
}
