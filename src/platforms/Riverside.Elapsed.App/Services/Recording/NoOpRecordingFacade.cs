using System.Diagnostics;

namespace Riverside.Elapsed.App.Services.Recording;

/// <summary>
/// No-op recording facade used on web/mobile heads where local capture is not yet
/// implemented. Tracks lifecycle so the UI can still demonstrate the pause/resume/stop flow.
/// </summary>
internal sealed class NoOpRecordingFacade : IRecordingFacade
{
	private readonly Stopwatch _stopwatch = new();
	private FacadeRecordingState _state = FacadeRecordingState.Unsupported;

	public FacadeRecordingState State => _state;

	public TimeSpan Duration => _stopwatch.Elapsed;

	public string? SourceName => null;

	public bool IsSupported => false;

	public event EventHandler? StateChanged;

	public Task StartAsync(CancellationToken cancellationToken = default)
	{
		_state = FacadeRecordingState.Recording;
		_stopwatch.Restart();
		Notify();
		return Task.CompletedTask;
	}

	public Task PauseAsync(CancellationToken cancellationToken = default)
	{
		if (_state == FacadeRecordingState.Recording)
		{
			_stopwatch.Stop();
			_state = FacadeRecordingState.Paused;
			Notify();
		}

		return Task.CompletedTask;
	}

	public Task ResumeAsync(CancellationToken cancellationToken = default)
	{
		if (_state == FacadeRecordingState.Paused)
		{
			_stopwatch.Start();
			_state = FacadeRecordingState.Recording;
			Notify();
		}

		return Task.CompletedTask;
	}

	public Task<FacadeRecordingResult> StopAsync(CancellationToken cancellationToken = default)
	{
		_stopwatch.Stop();
		_state = FacadeRecordingState.Stopped;
		Notify();
		return Task.FromResult(new FacadeRecordingResult(null, _stopwatch.Elapsed));
	}

	private void Notify() => StateChanged?.Invoke(this, EventArgs.Empty);
}
