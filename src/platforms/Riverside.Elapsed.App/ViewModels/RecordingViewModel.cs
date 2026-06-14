using System.Collections.ObjectModel;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Riverside.Elapsed.App.Models.Recording;
using Riverside.Elapsed.App.Services.Recording;
using Riverside.Elapsed.App.Services.Upload;

namespace Riverside.Elapsed.App.ViewModels;

public sealed partial class RecordingViewModel : ObservableObject, IDisposable
{
	private const int ThumbMaxWidth = 480;
	private const int ThumbMaxHeight = 300;

	private readonly IRecordingFacade _recording;
	private readonly ICaptureSourceProvider _sourceProvider;
	private readonly LapseService _lapse;
	private readonly DispatcherQueueTimer? _timer;
	private readonly DispatcherQueueTimer? _previewTimer;
	private readonly DispatcherQueueTimer? _sourceRefreshTimer;
	private bool _previewUpdating;
	private bool _sourceRefreshing;
	private Microsoft.UI.Xaml.Media.Imaging.WriteableBitmap? _previewBitmap;
	private string? _currentDraftId;

	[ObservableProperty]
	public partial CaptureSourceKind SelectedSourceKind { get; set; } = CaptureSourceKind.Screen;

	[ObservableProperty]
	public partial CaptureSource? SelectedSource { get; set; }

	[ObservableProperty]
	public partial RecordingPhase Phase { get; set; } = RecordingPhase.Setup;

	[ObservableProperty]
	public partial string ElapsedDisplay { get; set; } = "00:00:00";

	[ObservableProperty]
	public partial string? StatusMessage { get; set; }

	[ObservableProperty]
	public partial ImageSource? PreviewImage { get; set; }

	[ObservableProperty]
	public partial double UploadProgress { get; set; }

	[ObservableProperty]
	public partial string? UploadStatusText { get; set; }

	[ObservableProperty]
	public partial string PublishTitle { get; set; } = "";

	[ObservableProperty]
	public partial string PublishDescription { get; set; } = "";

	[ObservableProperty]
	public partial int PublishVisibilityIndex { get; set; }

	[ObservableProperty]
	public partial bool IsSignedIn { get; set; }

	[ObservableProperty]
	public partial string? UserDisplayName { get; set; }

	[ObservableProperty]
	public partial string? UserHandle { get; set; }

	[ObservableProperty]
	public partial ImageSource? UserProfilePicture { get; set; }

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

			_sourceRefreshTimer = dispatcher.CreateTimer();
			_sourceRefreshTimer.Interval = TimeSpan.FromMilliseconds(1500);
			_sourceRefreshTimer.Tick += (_, _) => _ = RefreshSourcesInPlaceAsync();
		}

		_ = InitializeAsync();

		CurrentSources.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasSources));
	}

	public ObservableCollection<CaptureSource> CurrentSources { get; } = [];

	public bool HasSources => CurrentSources.Count > 0;

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
		catch (Exception)
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
		UpdateSourceRefreshTimer();
	}

	private async Task RefreshSourcesInPlaceAsync()
	{
		if (_sourceRefreshing || Phase != RecordingPhase.Setup)
			return;

		_sourceRefreshing = true;
		try
		{
			var fresh = await _sourceProvider.GetSourcesAsync(SelectedSourceKind);
			Dictionary<string, CaptureSource> existingById = [];
			foreach (var s in CurrentSources)
				existingById[s.Id] = s;

			HashSet<string> freshIds = [];
			foreach (var src in fresh)
			{
				freshIds.Add(src.Id);
				if (existingById.TryGetValue(src.Id, out var existing))
				{
					existing.Name = src.Name;
					existing.Description = src.Description;
					existing.Resolution = src.Resolution;
					if (src.Icon is not null)
					{
						existing.Icon = src.Icon;
					}
				}
				else
				{
					CurrentSources.Add(src);
				}
			}

			for (int i = CurrentSources.Count - 1; i >= 0; i--)
			{
				if (!freshIds.Contains(CurrentSources[i].Id))
				{
					if (CurrentSources[i] == SelectedSource)
					{
						SelectedSource = null;
					}
					CurrentSources.RemoveAt(i);
				}
			}

			foreach (var source in CurrentSources)
			{
				try
				{
					await _sourceProvider.RefreshThumbnailAsync(source, ThumbMaxWidth, ThumbMaxHeight);
				}
				catch (Exception) { }
			}
		}
		catch (Exception) { }
		finally
		{
			_sourceRefreshing = false;
		}
	}

	private void UpdateSourceRefreshTimer()
	{
		if (_sourceRefreshTimer is null) return;

		if (Phase == RecordingPhase.Setup)
			_sourceRefreshTimer.Start();
		else
			_sourceRefreshTimer.Stop();
	}

	private void UpdatePreviewTimer()
	{
		if (_previewTimer is null) return;

		if (SelectedSource is not null && Phase is RecordingPhase.Active or RecordingPhase.Paused)
			_previewTimer.Start();
		else
			_previewTimer.Stop();

		UpdateSourceRefreshTimer();
	}

	private async Task RefreshPreviewAsync()
	{
		if (_previewUpdating || SelectedSource is null)
			return;

		_previewUpdating = true;
		try
		{
			CapturedFrame? frame = null;

			if (SelectedSource.Kind == CaptureSourceKind.Camera && Phase is RecordingPhase.Active or RecordingPhase.Paused)
			{
				var framePath = _recording.GetLatestFramePath();
				if (framePath is not null)
				{
					frame = await Task.Run(() =>
					{
						using var codec = SkiaSharp.SKCodec.Create(framePath);
						if (codec is null) return null;
						var info = codec.Info.WithColorType(SkiaSharp.SKColorType.Bgra8888).WithAlphaType(SkiaSharp.SKAlphaType.Premul);
						var pixels = new byte[info.RowBytes * info.Height];
						codec.GetPixels(info, System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(pixels, 0));
						return new CapturedFrame(pixels, info.Width, info.Height);
					}).ConfigureAwait(true);
				}
			}
			else
			{
				frame = await _sourceProvider.CaptureFrameAsync(SelectedSource).ConfigureAwait(true);
			}

			if (frame is not null)
				BlitPreview(frame);
		}
		catch (Exception) { }
		finally
		{
			_previewUpdating = false;
		}
	}

	private void BlitPreview(CapturedFrame frame)
	{
		int w = frame.Width;
		int h = frame.Height;

		if (_previewBitmap is null || _previewBitmap.PixelWidth != w || _previewBitmap.PixelHeight != h)
		{
			_previewBitmap = new Microsoft.UI.Xaml.Media.Imaging.WriteableBitmap(w, h);
			PreviewImage = _previewBitmap;
		}

		using var stream = _previewBitmap.PixelBuffer.AsStream();
		if (frame.IsBottomUp)
		{
			int stride = w * 4;
			for (int y = h - 1; y >= 0; y--)
				stream.Write(frame.Pixels, y * stride, stride);
		}
		else
		{
			stream.Write(frame.Pixels, 0, w * h * 4);
		}
		_previewBitmap.Invalidate();
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
				catch (Exception) { }
			}

			_timer?.Stop();
			_previewTimer?.Stop();
			_previewBitmap = null;

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
		_sourceRefreshTimer?.Stop();
		_recording.StateChanged -= OnRecordingStateChanged;
	}
}
