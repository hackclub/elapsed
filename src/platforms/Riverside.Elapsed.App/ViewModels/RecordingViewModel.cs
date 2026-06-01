using System.Collections.ObjectModel;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media;
using Riverside.Elapsed.App.Models.Recording;
using Riverside.Elapsed.App.Services.Recording;

namespace Riverside.Elapsed.App.ViewModels;

public sealed partial class RecordingViewModel : ObservableObject, IDisposable
{
	private readonly IRecordingFacade _recording;
	private readonly ICaptureSourceProvider _sourceProvider;
	private readonly DispatcherQueueTimer? _timer;
	private readonly DispatcherQueueTimer? _previewTimer;
	private bool _previewUpdating;

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

	public RecordingViewModel(IRecordingFacade recording, ICaptureSourceProvider sourceProvider)
	{
		_recording = recording;
		_sourceProvider = sourceProvider;
		_recording.StateChanged += OnRecordingStateChanged;

		StartRecordingCommand = new AsyncRelayCommand(StartRecordingAsync, () => Phase == RecordingPhase.Setup && SelectedSource is not null);
		PauseResumeCommand = new AsyncRelayCommand(TogglePauseResumeAsync, () => Phase is RecordingPhase.Active or RecordingPhase.Paused);
		StopCommand = new AsyncRelayCommand(StopAsync, () => Phase is RecordingPhase.Active or RecordingPhase.Paused);

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

		_ = RefreshSourcesAsync();
	}

	public ObservableCollection<CaptureSource> CurrentSources { get; } = [];

	public bool IsInSetup => Phase == RecordingPhase.Setup;

	public bool IsActive => Phase != RecordingPhase.Setup;

	public bool IsPaused => Phase == RecordingPhase.Paused;

	public string PauseResumeLabel => Phase == RecordingPhase.Paused ? "Resume" : "Pause";

	public string PauseResumeGlyph => Phase == RecordingPhase.Paused ? "" : "";

	public IAsyncRelayCommand StartRecordingCommand { get; }

	public IAsyncRelayCommand PauseResumeCommand { get; }

	public IAsyncRelayCommand StopCommand { get; }

	public event EventHandler? RecordingStarted;

	public event EventHandler? RecordingStopped;

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
		OnPropertyChanged(nameof(IsPaused));
		OnPropertyChanged(nameof(PauseResumeLabel));
		OnPropertyChanged(nameof(PauseResumeGlyph));
		StartRecordingCommand.NotifyCanExecuteChanged();
		PauseResumeCommand.NotifyCanExecuteChanged();
		StopCommand.NotifyCanExecuteChanged();
		UpdatePreviewTimer();
	}

	private async Task RefreshSourcesAsync()
	{
		CurrentSources.Clear();
		var sources = await _sourceProvider.GetSourcesAsync(SelectedSourceKind);
		Console.Error.WriteLine($"[Elapsed] {SelectedSourceKind}: {sources.Count} source(s)");
		foreach (var source in sources)
		{
			Console.Error.WriteLine($"[Elapsed]   - {source.Name} | thumb={source.Thumbnail is not null}");
			CurrentSources.Add(source);
		}
		SelectedSource = null;
	}

	private void UpdatePreviewTimer()
	{
		if (_previewTimer is null) return;

		if (SelectedSource is not null && Phase != RecordingPhase.Setup)
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
		catch { /* capture may fail transiently */ }
		finally
		{
			_previewUpdating = false;
		}
	}

	private async Task StartRecordingAsync()
	{
		try
		{
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
			var result = await _recording.StopAsync().ConfigureAwait(true);
			_timer?.Stop();
			Phase = RecordingPhase.Setup;
			ElapsedDisplay = "00:00:00";
			StatusMessage = result.FilePath is null
				? null
				: $"Saved to {result.FilePath}";
			RecordingStopped?.Invoke(this, EventArgs.Empty);
		}
		catch (Exception ex)
		{
			StatusMessage = $"Failed to stop: {ex.Message}";
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
