using Riverside.Elapsed.App.Models.Timelapses.Local;
using Riverside.Elapsed.App.Services.Drafts;

namespace Riverside.Elapsed.App.ViewModels.Timelapses.Drafts;

public sealed partial class DraftDetailsViewModel : ObservableObject
{
	private readonly ILocalDraftRepository _drafts;
	private readonly INavigator _navigator;

	private CancellationTokenSource? _autosaveCts;

	[ObservableProperty]
	private bool _isLoading;

	[ObservableProperty]
	private string? _errorMessage;

	[ObservableProperty]
	private LocalDraft? _draft;

	public DraftListItem Args { get; private set; }

	public string Name
	{
		get => Draft?.Name ?? "";
		set
		{
			if (Draft is null)
				return;
			if (Draft.Name == value)
				return;

			Draft = Draft with
			{
				Name = value,
				LastModifiedAt = DateTimeOffset.UtcNow,
			};

			OnPropertyChanged(nameof(Name));
			TriggerAutosave();
			SaveCommand.NotifyCanExecuteChanged();
		}
	}

	public string Description
	{
		get => Draft?.Description ?? "";
		set
		{
			if (Draft is null)
				return;
			if (Draft.Description == value)
				return;

			Draft = Draft with
			{
				Description = value,
				LastModifiedAt = DateTimeOffset.UtcNow,
			};

			OnPropertyChanged(nameof(Description));
			TriggerAutosave();
			SaveCommand.NotifyCanExecuteChanged();
		}
	}

	public IAsyncRelayCommand SaveCommand { get; }
	public IAsyncRelayCommand ReloadCommand { get; }

	public DraftDetailsViewModel(ILocalDraftRepository drafts, INavigator navigator)
	{
		_drafts = drafts;
		_navigator = navigator;

		SaveCommand = new AsyncRelayCommand(SaveAsync, CanSave);
		ReloadCommand = new AsyncRelayCommand(ReloadAsync);
	}

	public async Task OnNavigatedToAsync(DraftListItem args)
	{
		Args = args;
		await ReloadAsync();
	}

	private async Task ReloadAsync()
	{
		if (Args is null)
		{
			ErrorMessage = "Missing navigation arguments.";
			return;
		}

		try
		{
			ErrorMessage = null;
			IsLoading = true;

			var loaded = await _drafts.GetDraftAsync(Args.LocalDraftId);
			if (loaded is null)
			{
				ErrorMessage = "Draft not found (it may have been deleted).";
				Draft = null;
				return;
			}

			Draft = loaded;

			OnPropertyChanged(nameof(Name));
			OnPropertyChanged(nameof(Description));
			SaveCommand.NotifyCanExecuteChanged();
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

	private bool CanSave()
		=> Draft is not null;

	private async Task SaveAsync()
	{
		if (Draft is null)
			return;

		try
		{
			ErrorMessage = null;
			await _drafts.SaveDraftAsync(Draft);
		}
		catch (Exception ex)
		{
			ErrorMessage = ex.Message;
		}
	}

	private void TriggerAutosave()
	{
		_autosaveCts?.Cancel();
		_autosaveCts?.Dispose();

		_autosaveCts = new();
		var token = _autosaveCts.Token;

		_ = Task.Run(async () =>
		{
			try
			{
				await Task.Delay(TimeSpan.FromMilliseconds(500), token);
				if (token.IsCancellationRequested)
					return;

				await SaveAsync();
			}
			catch (OperationCanceledException) { }
		}, token);
	}
}
