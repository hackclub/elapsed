#if HAS_MEDIA_RECORDING
using System.Diagnostics;
using OwlCore.Storage.System.IO;
using Riverside.MediaRecording;
using Riverside.MediaRecording.Windows;

namespace Riverside.Elapsed.App.Services.Recording;

/// <summary>
/// Windows-backed recording facade that delegates screen capture to
/// <see cref="WindowsScreenCapture"/>. Targets the primary display and persists frames to
/// a per-session file inside <c>%LOCALAPPDATA%\Riverside\Elapsed\recordings</c>.
/// </summary>
internal sealed class WindowsRecordingFacade : IRecordingFacade, IAsyncDisposable
{
	private readonly WindowsScreenCapture _capture = new();
	private readonly Stopwatch _stopwatch = new();
	private readonly SemaphoreSlim _gate = new(1, 1);

	private IVideoCaptureSession? _session;
	private string? _activeOutputPath;
	private FacadeRecordingState _state = FacadeRecordingState.Idle;

	public FacadeRecordingState State => _state;

	public TimeSpan Duration => _stopwatch.Elapsed;

	public string? SourceName { get; private set; } = "Primary display";

	public bool IsSupported => true;

	public event EventHandler? StateChanged;

	public async Task StartAsync(CancellationToken cancellationToken = default)
	{
		await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
		try
		{
			if (_session is not null) return;

			var primary = _capture.Sources.FirstOrDefault(s => s.DeviceType == DeviceType.Display);
			if (primary.Id == Guid.Empty)
			{
				throw new InvalidOperationException("No display source available for capture.");
			}

			SourceName = primary.Name;

			var directory = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
				"Riverside", "Elapsed", "recordings");
			Directory.CreateDirectory(directory);

			_activeOutputPath = Path.Combine(directory, $"timelapse-{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.zip");

			// create the file so OwlCore can open it for writing.
			using (File.Create(_activeOutputPath)) { }

			var outputFile = new SystemFile(_activeOutputPath);
			_session = await _capture.CreateRecordingSessionAsync(primary, outputFile: outputFile, cancellationToken: cancellationToken)
				.ConfigureAwait(false);

			await _session.StartAsync(cancellationToken).ConfigureAwait(false);
			_stopwatch.Restart();
			_state = FacadeRecordingState.Recording;
			Notify();
		}
		finally
		{
			_gate.Release();
		}
	}

	public async Task PauseAsync(CancellationToken cancellationToken = default)
	{
		await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
		try
		{
			if (_session is null || _state != FacadeRecordingState.Recording) return;
			await _session.PauseAsync(cancellationToken).ConfigureAwait(false);
			_stopwatch.Stop();
			_state = FacadeRecordingState.Paused;
			Notify();
		}
		finally
		{
			_gate.Release();
		}
	}

	public async Task ResumeAsync(CancellationToken cancellationToken = default)
	{
		await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
		try
		{
			if (_session is null || _state != FacadeRecordingState.Paused) return;
			await _session.ResumeAsync(cancellationToken).ConfigureAwait(false);
			_stopwatch.Start();
			_state = FacadeRecordingState.Recording;
			Notify();
		}
		finally
		{
			_gate.Release();
		}
	}

	public async Task<FacadeRecordingResult> StopAsync(CancellationToken cancellationToken = default)
	{
		await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
		try
		{
			if (_session is null)
			{
				return new FacadeRecordingResult(null, TimeSpan.Zero);
			}

			var captured = await _session.StopAsync(cancellationToken).ConfigureAwait(false);
			_stopwatch.Stop();
			_state = FacadeRecordingState.Stopped;
			var path = _activeOutputPath;
			var duration = captured.Duration != TimeSpan.Zero ? captured.Duration : _stopwatch.Elapsed;
			_session = null;
			_activeOutputPath = null;
			Notify();
			return new FacadeRecordingResult(path, duration);
		}
		finally
		{
			_gate.Release();
		}
	}

	public async ValueTask DisposeAsync()
	{
		try
		{
			if (_session is not null)
			{
				await _session.StopAsync().ConfigureAwait(false);
			}
		}
		catch
		{
			// swallow: dispose is best-effort.
		}

		await _capture.DisposeAsync().ConfigureAwait(false);
		_gate.Dispose();
	}

	private void Notify() => StateChanged?.Invoke(this, EventArgs.Empty);
}
#endif
