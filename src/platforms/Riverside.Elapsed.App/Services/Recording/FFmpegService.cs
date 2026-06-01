#if HAS_MEDIA_RECORDING
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using SkiaSharp;

namespace Riverside.Elapsed.App.Services.Recording;

internal static partial class FFmpegService
{
	private static string? _cachedPath;

	public static string GetBinaryPath()
	{
		if (_cachedPath is not null)
			return _cachedPath;

		var exeName = OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg";

		var appDir = AppContext.BaseDirectory;
		var bundled = Path.Combine(appDir, exeName);
		if (File.Exists(bundled))
			return _cachedPath = bundled;

		if (OperatingSystem.IsMacOS())
		{
			foreach (var p in new[] { "/opt/homebrew/bin/ffmpeg", "/usr/local/bin/ffmpeg" })
				if (File.Exists(p)) return _cachedPath = p;
		}
		else if (OperatingSystem.IsLinux())
		{
			if (File.Exists("/usr/bin/ffmpeg"))
				return _cachedPath = "/usr/bin/ffmpeg";
		}

		var pathDirs = Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator) ?? [];
		foreach (var dir in pathDirs)
		{
			var candidate = Path.Combine(dir, exeName);
			if (File.Exists(candidate))
				return _cachedPath = candidate;
		}

		throw new InvalidOperationException(
			"FFmpeg is required but was not found. " +
			"Please install FFmpeg and ensure it is available on your system PATH, " +
			"or place the ffmpeg binary next to the application executable.");
	}

	public static Task<bool> IsAvailableAsync()
	{
		try
		{
			GetBinaryPath();
			return Task.FromResult(true);
		}
		catch
		{
			return Task.FromResult(false);
		}
	}

	public static async Task EncodeFramesToVideoAsync(
		string framesDirectory,
		string outputPath,
		double frameRate = 24.0,
		IProgress<double>? progress = null,
		CancellationToken ct = default)
	{
		var frameFiles = Directory.GetFiles(framesDirectory, "frame-*.jpg");
		if (frameFiles.Length == 0)
			throw new InvalidOperationException("No frames to encode.");

		int totalFrames = frameFiles.Length;
		var (maxW, maxH) = ScanMaxDimensions(frameFiles);

		maxW = RoundUpEven(maxW);
		maxH = RoundUpEven(maxH);

		var ffmpeg = GetBinaryPath();
		var inputPattern = Path.Combine(framesDirectory, "frame-%06d.jpg");

		var args = $"-y -framerate {frameRate} -i \"{inputPattern}\" " +
			$"-vf \"scale={maxW}:{maxH}:force_original_aspect_ratio=decrease,pad={maxW}:{maxH}:(ow-iw)/2:(oh-ih)/2:black\" " +
			$"-c:v libx264 -preset fast -crf 18 -pix_fmt yuv420p -movflags +faststart \"{outputPath}\"";

		var psi = new ProcessStartInfo
		{
			FileName = ffmpeg,
			Arguments = args,
			UseShellExecute = false,
			RedirectStandardError = true,
			CreateNoWindow = true,
		};

		using var proc = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start FFmpeg.");
		using var reg = ct.Register(() => { try { proc.Kill(); } catch { } });

		var stderr = new StringBuilder();
		proc.ErrorDataReceived += (_, e) =>
		{
			if (e.Data is null) return;
			stderr.AppendLine(e.Data);

			var match = FrameProgressRegex().Match(e.Data);
			if (match.Success && int.TryParse(match.Groups[1].Value, out int frame))
				progress?.Report(Math.Min(1.0, (double)frame / totalFrames));
		};
		proc.BeginErrorReadLine();

		await proc.WaitForExitAsync(ct);

		if (proc.ExitCode != 0)
			throw new InvalidOperationException($"FFmpeg exited with code {proc.ExitCode}:\n{stderr}");

		progress?.Report(1.0);
	}

	public static Process StartCameraCapture(
		string deviceId,
		string framesDirectory,
		int fps = 1)
	{
		var ffmpeg = GetBinaryPath();
		string inputFormat;
		string inputDevice;

		if (OperatingSystem.IsWindows())
		{
			inputFormat = "dshow";
			inputDevice = $"video={deviceId}";
		}
		else if (OperatingSystem.IsMacOS())
		{
			inputFormat = "avfoundation";
			inputDevice = deviceId;
		}
		else
		{
			inputFormat = "v4l2";
			inputDevice = deviceId;
		}

		var outputPattern = Path.Combine(framesDirectory, "frame-%06d.jpg");
		var args = $"-f {inputFormat} -i \"{inputDevice}\" -r {fps} -q:v 2 \"{outputPattern}\"";

		var psi = new ProcessStartInfo
		{
			FileName = ffmpeg,
			Arguments = args,
			UseShellExecute = false,
			RedirectStandardError = true,
			CreateNoWindow = true,
		};

		return Process.Start(psi) ?? throw new InvalidOperationException("Failed to start FFmpeg camera capture.");
	}

	public static async Task<string?> GrabCameraFrameAsync(string deviceId, string outputPath, CancellationToken ct = default)
	{
		var ffmpeg = GetBinaryPath();
		string inputFormat;
		string inputDevice;

		if (OperatingSystem.IsWindows())
		{
			inputFormat = "dshow";
			inputDevice = $"video={deviceId}";
		}
		else if (OperatingSystem.IsMacOS())
		{
			inputFormat = "avfoundation";
			inputDevice = deviceId;
		}
		else
		{
			inputFormat = "v4l2";
			inputDevice = deviceId;
		}

		var args = $"-f {inputFormat} -i \"{inputDevice}\" -vframes 1 -y \"{outputPath}\"";
		var psi = new ProcessStartInfo
		{
			FileName = ffmpeg,
			Arguments = args,
			UseShellExecute = false,
			RedirectStandardError = true,
			CreateNoWindow = true,
		};

		using var proc = Process.Start(psi);
		if (proc is null) return null;

		using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
		cts.CancelAfter(TimeSpan.FromSeconds(10));
		using var reg = cts.Token.Register(() => { try { proc.Kill(); } catch { } });

		await proc.WaitForExitAsync(cts.Token);
		return proc.ExitCode == 0 && File.Exists(outputPath) ? outputPath : null;
	}

	public static async Task<List<(string id, string name)>> EnumerateCamerasAsync(CancellationToken ct = default)
	{
		var ffmpeg = GetBinaryPath();
		string inputFormat;

		if (OperatingSystem.IsWindows())
			inputFormat = "dshow";
		else if (OperatingSystem.IsMacOS())
			inputFormat = "avfoundation";
		else
			inputFormat = "v4l2";

		var args = $"-f {inputFormat} -list_devices true -i dummy";
		var psi = new ProcessStartInfo
		{
			FileName = ffmpeg,
			Arguments = args,
			UseShellExecute = false,
			RedirectStandardError = true,
			RedirectStandardOutput = true,
			CreateNoWindow = true,
		};

		using var proc = Process.Start(psi);
		if (proc is null) return [];

		var stderr = await proc.StandardError.ReadToEndAsync(ct);
		await proc.WaitForExitAsync(ct);

		return ParseCameraDevices(stderr, inputFormat);
	}

	private static List<(string id, string name)> ParseCameraDevices(string stderr, string inputFormat)
	{
		var results = new List<(string, string)>();

		if (inputFormat == "dshow")
		{
			foreach (var line in stderr.Split('\n'))
			{
				if (!line.Contains("(video)")) continue;
				var match = DshowDeviceRegex().Match(line);
				if (match.Success)
				{
					var name = match.Groups[1].Value;
					results.Add((name, name));
				}
			}
		}
		else if (inputFormat == "avfoundation")
		{
			bool inVideo = false;
			foreach (var line in stderr.Split('\n'))
			{
				if (line.Contains("AVFoundation video devices"))
					inVideo = true;
				else if (line.Contains("AVFoundation audio devices"))
					break;
				else if (inVideo)
				{
					var match = AvfoundationDeviceRegex().Match(line);
					if (match.Success)
					{
						var index = match.Groups[1].Value;
						var name = match.Groups[2].Value;
						results.Add((index, name));
					}
				}
			}
		}
		else if (inputFormat == "v4l2")
		{
			for (int i = 0; i < 10; i++)
			{
				var devPath = $"/dev/video{i}";
				if (File.Exists(devPath))
					results.Add((devPath, $"Camera {i} ({devPath})"));
			}
		}

		return results;
	}

	private static (int maxW, int maxH) ScanMaxDimensions(string[] frameFiles)
	{
		int maxW = 0, maxH = 0;

		var sample = frameFiles.Length <= 10
			? frameFiles
			: new[] { frameFiles[0], frameFiles[frameFiles.Length / 2], frameFiles[^1] };

		foreach (var file in sample)
		{
			using var codec = SKCodec.Create(file);
			if (codec is null) continue;
			maxW = Math.Max(maxW, codec.Info.Width);
			maxH = Math.Max(maxH, codec.Info.Height);
		}

		if (maxW == 0 || maxH == 0)
		{
			maxW = 1920;
			maxH = 1080;
		}

		return (maxW, maxH);
	}

	private static int RoundUpEven(int value) => value % 2 == 0 ? value : value + 1;

	[GeneratedRegex(@"frame=\s*(\d+)")]
	private static partial Regex FrameProgressRegex();

	[GeneratedRegex(@"""(.+?)""")]
	private static partial Regex DshowDeviceRegex();

	[GeneratedRegex(@"\[(\d+)\]\s+(.+)")]
	private static partial Regex AvfoundationDeviceRegex();
}
#endif
