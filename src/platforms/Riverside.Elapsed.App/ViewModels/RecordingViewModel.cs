using System.Collections.ObjectModel;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Riverside.Elapsed.App.Models.Recording;
using Riverside.Elapsed.App.Services.Recording;
using Riverside.Elapsed.App.Services.Upload;

namespace Riverside.Elapsed.App.ViewModels;

public sealed partial class RecordingViewModel : ObservableObject, IDisposable
{
	private readonly IRecordingFacade _recording;
	private readonly ICaptureSourceProvider _sourceProvider;
	private readonly LapseService _lapse;
	private readonly DispatcherQueueTimer? _timer;
	private readonly DispatcherQueueTimer? _previewTimer;
	private bool _previewUpdating;
	private string? _currentDraftId;

	[ObservableProperty]
	private CaptureSourceKind _selectedSourceKind = CaptureSourceKind.Screen;

	[ObservableProperty]
	private CaptureSource? _selectedSource;

	[ObservableProperty]
	private RecordingPhase _phase = RecordingPhase.Setup;

	[ObservableProperty]
	private string _elapsedDisplay = "00:00:00";

	[ObservableProperty]
	private string? _statusMessage;

	[ObservableProperty]
	private ImageSource? _previewImage;

	[ObservableProperty]
	private double _uploadProgress;

	[ObservableProperty]
	private string? _uploadStatusText;

	[ObservableProperty]
	private string _publishTitle = "";

	[ObservableProperty]
	private string _publishDescription = "";

	[ObservableProperty]
	private int _publishVisibilityIndex;

	[ObservableProperty]
	private bool _isSignedIn;

	[ObservableProperty]
	private string? _userDisplayName;

	[ObservableProperty]
	private string? _userHandle;

	[ObservableProperty]
	private ImageSource? _userProfilePicture;

	public RecordingViewModel(IRecordingFacade recording, ICaptureSourceProvider sourceProvider, LapseService lapse)
	{
		_recording = recording;
		_sourceProvider = sourceProvider;
		_lapse = lapse;
		_recording.StateChanged += OnRecordingStateChanged;

		StartRecordingCommand = new AsyncRelayCommand(StartRecordingAsync, CanStartRecording);
		PauseResumeCommand = new AsyncRelayCommand(TogglePauseResumeAsync, () => Phase is RecordingPhase.Active or RecordingPhase.Paused);
		StopCommand = new AsyncRelayCommand(StopAsync, () => Phase is RecordingPhase.Active or RecordingPhase.Paused);
		PublishCommand = new AsyncRelayCommand(PublishAsync, () => Phase == RecordingPhase.Publishing);
		SignInCommand = new AsyncRelayCommand(SignInAsync, () => !IsSignedIn);
		SignOutCommand = new AsyncRelayCommand(SignOutAsync, () => IsSignedIn);
		ViewProfileCommand = new RelayCommand(ViewProfile, () => IsSignedIn);

		var dispatcher = DispatcherQueue.GetForCurrentThread();
		if (dispatcher is not null)
		{
			_timer = dispatcher.CreateTimer();
			_timer.Interval = TimeSpan.FromMilliseconds(500);
			_timer.Tick += (_, _) => RefreshElapsed();

			_previewTimer = dispatcher.CreateTimer();
			_previewTimer.Interval = TimeSpan.FromSeconds(1);
			_previewTimer.Tick += (_, _) => _ = RefreshPreviewAsync();
		}

		_ = InitializeAsync();
	}

	public ObservableCollection<CaptureSource> CurrentSources { get; } = [];

	public bool IsInSetup => Phase == RecordingPhase.Setup;

	public bool IsActive => Phase is RecordingPhase.Active or RecordingPhase.Paused;

	public bool IsEncoding => Phase == RecordingPhase.Encoding;

	public bool IsUploading => Phase == RecordingPhase.Uploading;

	public bool IsPublishing => Phase == RecordingPhase.Publishing;

	public bool IsPaused => Phase == RecordingPhase.Paused;

	public static string[] VisibilityOptions => ["Public", "Unlisted"];

	public string PauseResumeLabel => Phase == RecordingPhase.Paused ? "Resume" : "Pause";

	public string PauseResumeGlyph => Phase == RecordingPhase.Paused ? "" : "";

	public IAsyncRelayCommand StartRecordingCommand { get; }

	public IAsyncRelayCommand PauseResumeCommand { get; }

	public IAsyncRelayCommand StopCommand { get; }

	public IAsyncRelayCommand PublishCommand { get; }

	public IAsyncRelayCommand SignInCommand { get; }

	public IAsyncRelayCommand SignOutCommand { get; }

	public IRelayCommand ViewProfileCommand { get; }

	public event EventHandler? RecordingStarted;

	public event EventHandler? RecordingStopped;

	public event EventHandler? FocusRequested;

	private bool CanStartRecording()
		=> Phase == RecordingPhase.Setup && SelectedSource is not null && IsSignedIn;

	private async Task InitializeAsync()
	{
		await _lapse.InitializeAsync();
		if (_lapse.IsAuthenticated)
			await LoadUserProfileAsync();

		_ = RefreshSourcesAsync();
	}

	private async Task LoadUserProfileAsync()
	{
		try
		{
			var profile = await _lapse.GetCurrentUserAsync();
			if (profile is not null)
			{
				IsSignedIn = true;
				UserDisplayName = profile.DisplayName;
				UserHandle = profile.Handle;
				if (profile.ProfilePictureUrl is not null)
					UserProfilePicture = new BitmapImage(new Uri(profile.ProfilePictureUrl));
			}
			else
			{
				ClearUserState();
			}
		}
		catch
		{
			ClearUserState();
		}

		StartRecordingCommand.NotifyCanExecuteChanged();
		SignInCommand.NotifyCanExecuteChanged();
		SignOutCommand.NotifyCanExecuteChanged();
		ViewProfileCommand.NotifyCanExecuteChanged();
	}

	private void ClearUserState()
	{
		IsSignedIn = false;
		UserDisplayName = null;
		UserHandle = null;
		UserProfilePicture = null;
	}

	private async Task SignInAsync()
	{
		try
		{
			StatusMessage = null;
			await _lapse.SignInAsync();
			await LoadUserProfileAsync();
			FocusRequested?.Invoke(this, EventArgs.Empty);
		}
		catch (Exception ex)
		{
			StatusMessage = $"Sign in failed: {ex.Message}";
		}
	}

	private async Task SignOutAsync()
	{
		await _lapse.SignOutAsync();
		ClearUserState();
		StartRecordingCommand.NotifyCanExecuteChanged();
		SignInCommand.NotifyCanExecuteChanged();
		SignOutCommand.NotifyCanExecuteChanged();
		ViewProfileCommand.NotifyCanExecuteChanged();
	}

	private void ViewProfile()
	{
		if (UserHandle is not null)
			LapseService.OpenProfileInBrowser(UserHandle);
	}

	partial void OnSelectedSourceKindChanged(CaptureSourceKind value)
	{
		_ = RefreshSourcesAsync();
	}

	partial void OnSelectedSourceChanged(CaptureSource? value)
	{
		StartRecordingCommand.NotifyCanExecuteChanged();
		_ = RefreshPreviewAsync();
		UpdatePreviewTimer();
	}

	partial void OnPhaseChanged(RecordingPhase value)
	{
		OnPropertyChanged(nameof(IsInSetup));
		OnPropertyChanged(nameof(IsActive));
		OnPropertyChanged(nameof(IsEncoding));
		OnPropertyChanged(nameof(IsUploading));
		OnPropertyChanged(nameof(IsPublishing));
		OnPropertyChanged(nameof(IsPaused));
		OnPropertyChanged(nameof(PauseResumeLabel));
		OnPropertyChanged(nameof(PauseResumeGlyph));
		StartRecordingCommand.NotifyCanExecuteChanged();
		PauseResumeCommand.NotifyCanExecuteChanged();
		StopCommand.NotifyCanExecuteChanged();
		PublishCommand.NotifyCanExecuteChanged();
		UpdatePreviewTimer();
	}

	private async Task RefreshSourcesAsync()
	{
		CurrentSources.Clear();
		var sources = await _sourceProvider.GetSourcesAsync(SelectedSourceKind);
		foreach (var source in sources)
			CurrentSources.Add(source);
		SelectedSource = null;
	}

	private void UpdatePreviewTimer()
	{
		if (_previewTimer is null) return;

		if (SelectedSource is not null && Phase is RecordingPhase.Active or RecordingPhase.Paused)
			_previewTimer.Start();
		else
			_previewTimer.Stop();
	}

	private async Task RefreshPreviewAsync()
	{
		if (_previewUpdating || SelectedSource is null)
			return;

		_previewUpdating = true;
		try
		{
			var image = await _sourceProvider.CapturePreviewAsync(SelectedSource, 640, 480);
			if (image is not null)
				PreviewImage = image;
		}
		catch { }
		finally
		{
			_previewUpdating = false;
		}
	}

	private async Task StartRecordingAsync()
	{
		try
		{
			_recording.SetSource(SelectedSource!);
			await _recording.StartAsync().ConfigureAwait(true);
			Phase = RecordingPhase.Active;
			_timer?.Start();
			_ = RefreshPreviewAsync();
			RecordingStarted?.Invoke(this, EventArgs.Empty);
		}
		catch (Exception ex)
		{
			StatusMessage = $"Failed to start: {ex.Message}";
		}
	}

	private async Task TogglePauseResumeAsync()
	{
		try
		{
			if (Phase == RecordingPhase.Paused)
			{
				await _recording.ResumeAsync().ConfigureAwait(true);
				Phase = RecordingPhase.Active;
				_timer?.Start();
			}
			else
			{
				await _recording.PauseAsync().ConfigureAwait(true);
				Phase = RecordingPhase.Paused;
				_timer?.Stop();
			}
		}
		catch (Exception ex)
		{
			StatusMessage = $"Failed: {ex.Message}";
		}
	}

	private async Task StopAsync()
	{
		try
		{
			byte[]? thumbnailBytes = null;
			if (SelectedSource is not null)
			{
				try
				{
					thumbnailBytes = await _sourceProvider.CapturePreviewBytesAsync(SelectedSource, 640, 480);
				}
				catch { }
			}

			_timer?.Stop();
			_previewTimer?.Stop();

			Phase = RecordingPhase.Encoding;
			RecordingStopped?.Invoke(this, EventArgs.Empty);

			var result = await _recording.StopAsync().ConfigureAwait(true);

			if (result.FilePath is null)
			{
				Phase = RecordingPhase.Setup;
				ElapsedDisplay = "00:00:00";
				return;
			}

			Phase = RecordingPhase.Uploading;

			try
			{
				var progress = new Progress<UploadProgress>(p =>
				{
					UploadProgress = p.Fraction;
					UploadStatusText = p.Description;
				});

				var draftId = await _lapse.UploadDraftAsync(
					result.FilePath,
					thumbnailBytes ?? [],
					result.Duration,
					progress);

				_currentDraftId = draftId;
				PublishTitle = "";
				PublishDescription = "";
				PublishVisibilityIndex = 0;
				StatusMessage = null;
				Phase = RecordingPhase.Publishing;
			}
			catch (Exception ex)
			{
				StatusMessage = $"Upload failed: {ex.Message}";
				Phase = RecordingPhase.Setup;
				ElapsedDisplay = "00:00:00";
				UploadProgress = 0;
				UploadStatusText = null;
			}
		}
		catch (Exception ex)
		{
			StatusMessage = $"Failed to stop: {ex.Message}";
		}
	}

	private async Task PublishAsync()
	{
		if (_currentDraftId is null) return;

		var title = string.IsNullOrWhiteSpace(PublishTitle) ? "Untitled Timelapse" : PublishTitle.Trim();
		var description = string.IsNullOrWhiteSpace(PublishDescription) ? null : PublishDescription.Trim();
		var visibility = PublishVisibilityIndex == 0 ? "PUBLIC" : "UNLISTED";

		StatusMessage = null;

		try
		{
			UploadStatusText = "Updating draft...";
			await _lapse.UpdateDraftAsync(_currentDraftId, title, description);

			UploadStatusText = "Publishing...";
			var timelapseId = await _lapse.PublishDraftAsync(_currentDraftId, visibility);

			LapseService.OpenTimelapseInBrowser(timelapseId);

			_currentDraftId = null;
			Phase = RecordingPhase.Setup;
			ElapsedDisplay = "00:00:00";
			UploadProgress = 0;
			UploadStatusText = null;
		}
		catch (Exception ex)
		{
			StatusMessage = $"Publish failed: {ex.Message}";
			UploadStatusText = null;
		}
	}

	private void OnRecordingStateChanged(object? sender, EventArgs e)
	{
		RefreshElapsed();
	}

	private void RefreshElapsed()
	{
		var elapsed = _recording.Duration;
		ElapsedDisplay = elapsed.ToString(@"hh\:mm\:ss");
	}

	public void Dispose()
	{
		_timer?.Stop();
		_previewTimer?.Stop();
		_recording.StateChanged -= OnRecordingStateChanged;
	}
}
