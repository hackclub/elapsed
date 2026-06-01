#if HAS_MEDIA_RECORDING
using System.Diagnostics;
using Riverside.Elapsed.App.Models.Recording;
using SkiaSharp;

namespace Riverside.Elapsed.App.Services.Recording;

internal sealed class TimelapseRecordingFacade : IRecordingFacade
{
	private readonly ICaptureSourceProvider _sourceProvider;
	private readonly Stopwatch _stopwatch = new();

	private CaptureSource? _source;
	private string? _framesDirectory;
	private CancellationTokenSource? _captureCts;
	private Task? _captureLoop;
	private Process? _cameraProcess;
	private int _frameCount;
	private FacadeRecordingState _state = FacadeRecordingState.Idle;
	private bool _paused;

	public TimelapseRecordingFacade(ICaptureSourceProvider sourceProvider)
	{
		_sourceProvider = sourceProvider;
	}

	public FacadeRecordingState State => _state;
	public TimeSpan Duration => _stopwatch.Elapsed;
	public string? SourceName { get; private set; }
	public bool IsSupported => true;
	public event EventHandler? StateChanged;

	public void SetSource(CaptureSource source)
	{
		_source = source;
		SourceName = source.Name;
	}

	public string? GetLatestFramePath()
	{
		if (_framesDirectory is null || !Directory.Exists(_framesDirectory))
			return null;

		var files = Directory.GetFiles(_framesDirectory, "frame-*.jpg");
		if (files.Length == 0) return null;

		Array.Sort(files);
		return files[^1];
	}

	public async Task StartAsync(CancellationToken cancellationToken = default)
	{
		if (_source is null)
			throw new InvalidOperationException("No capture source set. Call SetSource before starting.");

		if (!await FFmpegService.IsAvailableAsync())
			throw new InvalidOperationException(
				"FFmpeg is required but was not found. " +
				"Please install FFmpeg and ensure it is available on your system PATH.");

		var recordingId = Guid.NewGuid().ToString("N");
		_framesDirectory = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
			"Riverside", "Elapsed", "recordings", recordingId);
		Directory.CreateDirectory(_framesDirectory);

		_frameCount = 0;
		_paused = false;
		_captureCts = new CancellationTokenSource();

		if (_source.Kind == CaptureSourceKind.Camera)
		{
			_cameraProcess = FFmpegService.StartCameraCapture(_source.Id, _framesDirectory);
		}
		else
		{
			_captureLoop = Task.Run(() => CaptureLoopAsync(_captureCts.Token));
		}

		_stopwatch.Restart();
		_state = FacadeRecordingState.Recording;
		StateChanged?.Invoke(this, EventArgs.Empty);
	}

	public Task PauseAsync(CancellationToken cancellationToken = default)
	{
		_paused = true;
		_stopwatch.Stop();
		_state = FacadeRecordingState.Paused;
		StateChanged?.Invoke(this, EventArgs.Empty);
		return Task.CompletedTask;
	}

	public Task ResumeAsync(CancellationToken cancellationToken = default)
	{
		_paused = false;
		_stopwatch.Start();
		_state = FacadeRecordingState.Recording;
		StateChanged?.Invoke(this, EventArgs.Empty);
		return Task.CompletedTask;
	}

	public async Task<FacadeRecordingResult> StopAsync(CancellationToken cancellationToken = default)
	{
		_stopwatch.Stop();

		if (_cameraProcess is not null)
		{
			try
			{
				if (!_cameraProcess.HasExited)
					_cameraProcess.Kill();
				await _cameraProcess.WaitForExitAsync(cancellationToken);
			}
			catch { }
			_cameraProcess.Dispose();
			_cameraProcess = null;
		}

		if (_captureCts is not null)
		{
			await _captureCts.CancelAsync();
			if (_captureLoop is not null)
			{
				try { await _captureLoop; } catch (OperationCanceledException) { }
			}
			_captureCts.Dispose();
			_captureCts = null;
			_captureLoop = null;
		}

		var duration = _stopwatch.Elapsed;
		string? mp4Path = null;

		int actualFrameCount = _framesDirectory is not null
			? Directory.GetFiles(_framesDirectory, "frame-*.jpg").Length
			: 0;

		if (_framesDirectory is not null && actualFrameCount > 0)
		{
			mp4Path = Path.Combine(
				Path.GetDirectoryName(_framesDirectory)!,
				$"timelapse-{DateTime.Now:yyyyMMdd-HHmmss}.mp4");

			await FFmpegService.EncodeFramesToVideoAsync(
				_framesDirectory, mp4Path, frameRate: 24.0, ct: cancellationToken);

			try { Directory.Delete(_framesDirectory, recursive: true); } catch { }
		}
		else if (_framesDirectory is not null)
		{
			try { Directory.Delete(_framesDirectory, recursive: true); } catch { }
		}

		_framesDirectory = null;
		_state = FacadeRecordingState.Idle;
		StateChanged?.Invoke(this, EventArgs.Empty);

		return new FacadeRecordingResult(mp4Path, duration);
	}

	private async Task CaptureLoopAsync(CancellationToken ct)
	{
		var interval = TimeSpan.FromSeconds(1);
		int consecutiveFailures = 0;

		while (!ct.IsCancellationRequested)
		{
			if (!_paused && _source is not null)
			{
				try
				{
					var frame = await _sourceProvider.CaptureFrameAsync(_source);
					if (frame is not null)
					{
						SaveFrameAsJpeg(frame);
						consecutiveFailures = 0;
					}
					else
					{
						consecutiveFailures++;
					}
				}
				catch
				{
					consecutiveFailures++;
				}
			}

			try { await Task.Delay(interval, ct); }
			catch (OperationCanceledException) { break; }
		}
	}

	private void SaveFrameAsJpeg(CapturedFrame frame)
	{
		_frameCount++;
		var fileName = $"frame-{_frameCount:D6}.jpg";
		var filePath = Path.Combine(_framesDirectory!, fileName);

		var info = new SKImageInfo(frame.Width, frame.Height, SKColorType.Bgra8888, SKAlphaType.Opaque);
		using var bitmap = new SKBitmap(info);

		var pixels = frame.Pixels;
		if (frame.IsBottomUp)
		{
			int stride = frame.Width * 4;
			var flipped = new byte[pixels.Length];
			for (int y = 0; y < frame.Height; y++)
			{
				var srcRow = pixels.AsSpan(y * stride, stride);
				var dstRow = flipped.AsSpan((frame.Height - 1 - y) * stride, stride);
				srcRow.CopyTo(dstRow);
			}
			pixels = flipped;
		}

		unsafe
		{
			fixed (byte* ptr = pixels)
			{
				bitmap.InstallPixels(info, (nint)ptr, info.RowBytes);
				using var image = SKImage.FromBitmap(bitmap);
				using var data = image.Encode(SKEncodedImageFormat.Jpeg, 90);
				using var stream = File.OpenWrite(filePath);
				data.SaveTo(stream);
			}
		}
	}
}
#endif
