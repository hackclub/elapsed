using Microsoft.UI.Dispatching;
using Riverside.Elapsed.App.Services.Recording;

namespace Riverside.Elapsed.App.ViewModels;

/// <summary>
/// View-model that drives <c>RecordingPage</c>. Owns the active <see cref="IRecordingFacade"/>
/// session lifecycle (start → pause/resume → stop) and exposes elapsed-time updates to the UI.
/// </summary>
public sealed partial class RecordingViewModel : ObservableObject, IDisposable
{
	private readonly INavigator _navigator;
	private readonly IRecordingFacade _recording;
	private readonly DispatcherQueueTimer? _timer;

	[ObservableProperty]
	private string _status = "Preparing…";

	[ObservableProperty]
	private bool _isPaused;

	[ObservableProperty]
	private bool _isRecording;

	[ObservableProperty]
	private string _elapsedDisplay = "00:00:00";

	[ObservableProperty]
	private string? _sourceName;

	[ObservableProperty]
	private bool _isSupported = true;

	[ObservableProperty]
	private string? _lastOutputPath;

	public RecordingViewModel(INavigator navigator, IRecordingFacade recording)
	{
		_navigator = navigator;
		_recording = recording;
		_recording.StateChanged += OnRecordingStateChanged;

		SourceName = _recording.SourceName;
		IsSupported = _recording.IsSupported;

		StartCommand = new AsyncRelayCommand(StartAsync);
		PauseResumeCommand = new AsyncRelayCommand(TogglePauseResumeAsync);
		StopCommand = new AsyncRelayCommand(StopAsync);
		BackCommand = new AsyncRelayCommand(BackAsync);

		// drive the elapsed display from a UI-thread timer so the view sees consistent ticks.
		var dispatcher = DispatcherQueue.GetForCurrentThread();
		if (dispatcher is not null)
		{
			_timer = dispatcher.CreateTimer();
			_timer.Interval = TimeSpan.FromMilliseconds(500);
			_timer.Tick += (_, _) => RefreshElapsed();
			_timer.Start();
		}
	}

	public IAsyncRelayCommand StartCommand { get; }

	public IAsyncRelayCommand PauseResumeCommand { get; }

	public IAsyncRelayCommand StopCommand { get; }

	public IAsyncRelayCommand BackCommand { get; }

	public string PauseResumeText => IsPaused ? "Resume" : "Pause";

	partial void OnIsPausedChanged(bool value) => OnPropertyChanged(nameof(PauseResumeText));

	private async Task StartAsync()
	{
		try
		{
			Status = _recording.IsSupported ? "Starting recording…" : "Recording not supported on this platform";
			await _recording.StartAsync().ConfigureAwait(true);
		}
		catch (Exception ex)
		{
			Status = $"Failed to start: {ex.Message}";
		}
	}

	private async Task TogglePauseResumeAsync()
	{
		try
		{
			if (IsPaused)
			{
				await _recording.ResumeAsync().ConfigureAwait(true);
			}
			else
			{
				await _recording.PauseAsync().ConfigureAwait(true);
			}
		}
		catch (Exception ex)
		{
			Status = $"Failed: {ex.Message}";
		}
	}

	private async Task StopAsync()
	{
		try
		{
			Status = "Finalising…";
			var result = await _recording.StopAsync().ConfigureAwait(true);
			LastOutputPath = result.FilePath;
			Status = result.FilePath is null
				? "Recording stopped (no output)"
				: $"Saved to {result.FilePath}";
			IsRecording = false;
			IsPaused = false;
		}
		catch (Exception ex)
		{
			Status = $"Failed to stop: {ex.Message}";
		}
	}

	private async Task BackAsync()
	{
		try
		{
			if (_recording.State is FacadeRecordingState.Recording or FacadeRecordingState.Paused)
			{
				await _recording.StopAsync().ConfigureAwait(true);
			}
		}
		catch
		{
			// swallow: navigation should not be blocked by a failed stop.
		}

		await _navigator.NavigateBackAsync(this).ConfigureAwait(true);
	}

	private void OnRecordingStateChanged(object? sender, EventArgs e)
	{
		IsRecording = _recording.State == FacadeRecordingState.Recording;
		IsPaused = _recording.State == FacadeRecordingState.Paused;
		Status = _recording.State switch
		{
			FacadeRecordingState.Idle => "Idle",
			FacadeRecordingState.Recording => "Recording active",
			FacadeRecordingState.Paused => "Recording paused",
			FacadeRecordingState.Stopped => Status, // keep last detailed message (e.g. saved path).
			FacadeRecordingState.Unsupported => "Recording not supported on this platform",
			_ => Status,
		};
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
		_recording.StateChanged -= OnRecordingStateChanged;
	}
}
