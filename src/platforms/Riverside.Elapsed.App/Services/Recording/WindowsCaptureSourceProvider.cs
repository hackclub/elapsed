#if HAS_MEDIA_RECORDING
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.UI.Xaml.Media.Imaging;
using Riverside.Elapsed.App.Models.Recording;
using SkiaSharp;

namespace Riverside.Elapsed.App.Services.Recording;

public sealed class WindowsCaptureSourceProvider : ICaptureSourceProvider
{
	private const int ThumbMaxWidth = 480;
	private const int ThumbMaxHeight = 300;

	public async Task<IReadOnlyList<CaptureSource>> GetSourcesAsync(CaptureSourceKind kind)
	{
		if (kind == CaptureSourceKind.Camera)
			return await EnumerateCamerasAsync();

		var items = kind switch
		{
			CaptureSourceKind.Screen => EnumerateScreens(),
			CaptureSourceKind.Window => EnumerateWindows(),
			_ => []
		};

		foreach (var (source, pixels, tw, th) in items)
		{
			if (pixels is not null)
				source.Thumbnail = CreateThumbnail(pixels, tw, th);
		}

		return items.ConvertAll(i => i.source);
	}

	public async Task<Microsoft.UI.Xaml.Media.ImageSource?> CapturePreviewAsync(CaptureSource source, int maxWidth, int maxHeight)
	{
		var tempPath = await Task.Run(() =>
		{
			byte[]? pixels = null;
			int tw = 0, th = 0;

			if (source.Kind == CaptureSourceKind.Screen)
			{
				int index = 0;
				int targetIndex = int.Parse(source.Id.Replace("monitor-", ""));
				Native.MonitorEnumProc callback = (nint hMonitor, nint hdcMonitor, ref Native.RECT lprcMonitor, nint dwData) =>
				{
					if (index == targetIndex)
					{
						var mi = new Native.MONITORINFOEX();
						mi.cbSize = Marshal.SizeOf<Native.MONITORINFOEX>();
						if (Native.GetMonitorInfoW(hMonitor, ref mi))
						{
							int w = mi.rcMonitor.right - mi.rcMonitor.left;
							int h = mi.rcMonitor.bottom - mi.rcMonitor.top;
							(tw, th) = ScaleToFit(w, h, maxWidth, maxHeight);
							pixels = CaptureScreenPixels(mi.rcMonitor.left, mi.rcMonitor.top, w, h, tw, th);
						}
						index++;
						return false;
					}
					index++;
					return true;
				};
				Native.EnumDisplayMonitors(nint.Zero, nint.Zero, callback, nint.Zero);
				GC.KeepAlive(callback);
			}
			else if (source.Kind == CaptureSourceKind.Window)
			{
				var hWnd = nint.Parse(source.Id.Replace("window-", ""));
				Native.GetWindowRect(hWnd, out var rect);
				int w = rect.right - rect.left;
				int h = rect.bottom - rect.top;
				if (w > 1 && h > 1)
				{
					(tw, th) = ScaleToFit(w, h, maxWidth, maxHeight);
					pixels = CaptureWindowPixels(hWnd, w, h, tw, th);
				}
			}

			if (pixels is null)
				return null;

			var bmpData = EncodeBmp(pixels, tw, th);
			var path = Path.Combine(Path.GetTempPath(), $"elapsed-preview-{Guid.NewGuid():N}.bmp");
			File.WriteAllBytes(path, bmpData);
			return path;
		}).ConfigureAwait(true);

		if (tempPath is null)
			return null;

		return new BitmapImage(new Uri(tempPath));
	}

	public Task<CapturedFrame?> CaptureFrameAsync(CaptureSource source)
	{
		return Task.Run(() =>
		{
			if (source.Kind == CaptureSourceKind.Screen)
			{
				byte[]? pixels = null;
				int capturedW = 0, capturedH = 0;
				int index = 0;
				int targetIndex = int.Parse(source.Id.Replace("monitor-", ""));
				Native.MonitorEnumProc callback = (nint hMonitor, nint hdcMonitor, ref Native.RECT lprcMonitor, nint dwData) =>
				{
					if (index == targetIndex)
					{
						var mi = new Native.MONITORINFOEX();
						mi.cbSize = Marshal.SizeOf<Native.MONITORINFOEX>();
						if (Native.GetMonitorInfoW(hMonitor, ref mi))
						{
							capturedW = mi.rcMonitor.right - mi.rcMonitor.left;
							capturedH = mi.rcMonitor.bottom - mi.rcMonitor.top;
							pixels = CaptureScreenPixels(mi.rcMonitor.left, mi.rcMonitor.top, capturedW, capturedH, capturedW, capturedH);
						}
						index++;
						return false;
					}
					index++;
					return true;
				};
				Native.EnumDisplayMonitors(nint.Zero, nint.Zero, callback, nint.Zero);
				GC.KeepAlive(callback);
				return pixels is null ? null : new CapturedFrame(pixels, capturedW, capturedH, IsBottomUp: true);
			}
			else if (source.Kind == CaptureSourceKind.Window)
			{
				var hWnd = nint.Parse(source.Id.Replace("window-", ""));
				Native.GetWindowRect(hWnd, out var rect);
				int w = rect.right - rect.left;
				int h = rect.bottom - rect.top;
				if (w <= 1 || h <= 1) return null;

				var pixels = CaptureWindowPixels(hWnd, w, h, w, h);
				return pixels is null ? null : new CapturedFrame(pixels, w, h, IsBottomUp: true);
			}

			return (CapturedFrame?)null;
		});
	}

	public Task<byte[]?> CapturePreviewBytesAsync(CaptureSource source, int maxWidth, int maxHeight)
	{
		return Task.Run(() =>
		{
			byte[]? pixels = null;
			int tw = 0, th = 0;

			if (source.Kind == CaptureSourceKind.Screen)
			{
				int index = 0;
				int targetIndex = int.Parse(source.Id.Replace("monitor-", ""));
				Native.MonitorEnumProc callback = (nint hMonitor, nint hdcMonitor, ref Native.RECT lprcMonitor, nint dwData) =>
				{
					if (index == targetIndex)
					{
						var mi = new Native.MONITORINFOEX();
						mi.cbSize = Marshal.SizeOf<Native.MONITORINFOEX>();
						if (Native.GetMonitorInfoW(hMonitor, ref mi))
						{
							int w = mi.rcMonitor.right - mi.rcMonitor.left;
							int h = mi.rcMonitor.bottom - mi.rcMonitor.top;
							(tw, th) = ScaleToFit(w, h, maxWidth, maxHeight);
							pixels = CaptureScreenPixels(mi.rcMonitor.left, mi.rcMonitor.top, w, h, tw, th);
						}
						index++;
						return false;
					}
					index++;
					return true;
				};
				Native.EnumDisplayMonitors(nint.Zero, nint.Zero, callback, nint.Zero);
				GC.KeepAlive(callback);
			}
			else if (source.Kind == CaptureSourceKind.Window)
			{
				var hWnd = nint.Parse(source.Id.Replace("window-", ""));
				Native.GetWindowRect(hWnd, out var rect);
				int w = rect.right - rect.left;
				int h = rect.bottom - rect.top;
				if (w > 1 && h > 1)
				{
					(tw, th) = ScaleToFit(w, h, maxWidth, maxHeight);
					pixels = CaptureWindowPixels(hWnd, w, h, tw, th);
				}
			}

			if (pixels is null)
				return null;

			return (byte[]?)EncodeBmp(pixels, tw, th);
		});
	}

	private List<(CaptureSource source, byte[]? pixels, int tw, int th)> EnumerateScreens()
	{
		var results = new List<(CaptureSource, byte[]?, int, int)>();
		int index = 0;

		Native.MonitorEnumProc callback = (nint hMonitor, nint hdcMonitor, ref Native.RECT lprcMonitor, nint dwData) =>
		{
			var mi = new Native.MONITORINFOEX();
			mi.cbSize = Marshal.SizeOf<Native.MONITORINFOEX>();
			if (!Native.GetMonitorInfoW(hMonitor, ref mi))
				return true;

			int w = mi.rcMonitor.right - mi.rcMonitor.left;
			int h = mi.rcMonitor.bottom - mi.rcMonitor.top;
			int hz = GetMonitorRefreshRate(mi.szDevice);
			bool isPrimary = (mi.dwFlags & 1) != 0;

			var (tw, th) = ScaleToFit(w, h);
			var pixels = CaptureScreenPixels(mi.rcMonitor.left, mi.rcMonitor.top, w, h, tw, th);

			results.Add((new CaptureSource
			{
				Id = $"monitor-{index}",
				Name = isPrimary ? "Primary Display" : $"Display {index + 1}",
				Description = mi.szDevice.TrimEnd('\0'),
				Resolution = hz > 0 ? $"{w}x{h} @ {hz} Hz" : $"{w}x{h}",
				Kind = CaptureSourceKind.Screen,
			}, pixels, tw, th));

			index++;
			return true;
		};

		Native.EnumDisplayMonitors(nint.Zero, nint.Zero, callback, nint.Zero);
		GC.KeepAlive(callback);
		return results;
	}

	private List<(CaptureSource source, byte[]? pixels, int tw, int th)> EnumerateWindows()
	{
		var results = new List<(CaptureSource, byte[]?, int, int)>();
		int ownPid = Environment.ProcessId;

		Native.EnumWindowsProc callback = (nint hWnd, nint lParam) =>
		{
			if (!Native.IsWindowVisible(hWnd))
				return true;

			int exStyle = Native.GetWindowLongW(hWnd, -20);
			if ((exStyle & 0x00000080) != 0)
				return true;

			if (Native.DwmGetWindowAttribute(hWnd, 14, out int cloaked, 4) == 0 && cloaked != 0)
				return true;

			int textLen = Native.GetWindowTextLengthW(hWnd);
			if (textLen == 0)
				return true;

			var buf = new StringBuilder(textLen + 1);
			Native.GetWindowTextW(hWnd, buf, buf.Capacity);
			string title = buf.ToString();

			Native.GetWindowThreadProcessId(hWnd, out uint pid);
			if ((int)pid == ownPid)
				return true;

			string processName = "";
			try
			{
				using var proc = Process.GetProcessById((int)pid);
				processName = proc.ProcessName;
			}
			catch { /* process may have exited */ }

			Native.GetWindowRect(hWnd, out var rect);
			int w = rect.right - rect.left;
			int h = rect.bottom - rect.top;
			if (w <= 1 || h <= 1)
				return true;

			int hz = 0;
			nint hMon = Native.MonitorFromWindow(hWnd, 2);
			var monInfo = new Native.MONITORINFOEX();
			monInfo.cbSize = Marshal.SizeOf<Native.MONITORINFOEX>();
			if (Native.GetMonitorInfoW(hMon, ref monInfo))
				hz = GetMonitorRefreshRate(monInfo.szDevice);

			var (tw, th) = ScaleToFit(w, h);
			var pixels = CaptureWindowPixels(hWnd, w, h, tw, th);

			results.Add((new CaptureSource
			{
				Id = $"window-{hWnd}",
				Name = title,
				Description = processName,
				Resolution = hz > 0 ? $"{w}x{h} @ {hz} Hz" : $"{w}x{h}",
				Kind = CaptureSourceKind.Window,
				Icon = ExtractWindowIcon(hWnd),
			}, pixels, tw, th));

			return true;
		};

		Native.EnumWindows(callback, nint.Zero);
		GC.KeepAlive(callback);
		return results;
	}

	private static async Task<List<CaptureSource>> EnumerateCamerasAsync()
	{
		try
		{
			var cameras = await FFmpegService.EnumerateCamerasAsync();
			var results = new List<CaptureSource>();

			foreach (var (id, name) in cameras)
			{
				var source = new CaptureSource
				{
					Id = id,
					Name = name,
					Kind = CaptureSourceKind.Camera,
				};

				try
				{
					var thumbPath = Path.Combine(Path.GetTempPath(), $"elapsed-cam-{Guid.NewGuid():N}.jpg");
					var grabbed = await FFmpegService.GrabCameraFrameAsync(id, thumbPath);
					if (grabbed is not null)
					{
						using var codec = SkiaSharp.SKCodec.Create(thumbPath);
						if (codec is not null)
							source.Resolution = $"{codec.Info.Width}x{codec.Info.Height}";

						source.Thumbnail = new BitmapImage(new Uri(thumbPath));
					}
				}
				catch { }

				results.Add(source);
			}

			return results;
		}
		catch
		{
			return [];
		}
	}

	private static int GetMonitorRefreshRate(string deviceName)
	{
		var dm = new Native.DEVMODE();
		dm.dmSize = (ushort)Marshal.SizeOf<Native.DEVMODE>();
		return Native.EnumDisplaySettingsW(deviceName, -1, ref dm)
			? (int)dm.dmDisplayFrequency
			: 0;
	}

	private static byte[]? CaptureScreenPixels(int srcX, int srcY, int srcW, int srcH, int tw, int th)
	{
		nint hdcScreen = Native.GetDC(nint.Zero);
		nint hdcMem = Native.CreateCompatibleDC(hdcScreen);
		nint hBmp = Native.CreateCompatibleBitmap(hdcScreen, tw, th);
		nint hOld = Native.SelectObject(hdcMem, hBmp);

		Native.SetStretchBltMode(hdcMem, 4);
		Native.StretchBlt(hdcMem, 0, 0, tw, th, hdcScreen, srcX, srcY, srcW, srcH, 0x00CC0020);

		var pixels = ExtractPixels(hdcMem, hBmp, tw, th);

		Native.SelectObject(hdcMem, hOld);
		Native.DeleteObject(hBmp);
		Native.DeleteDC(hdcMem);
		Native.ReleaseDC(nint.Zero, hdcScreen);
		return pixels;
	}

	private static byte[]? CaptureWindowPixels(nint hWnd, int winW, int winH, int tw, int th)
	{
		nint hdcScreen = Native.GetDC(nint.Zero);

		nint hdcFull = Native.CreateCompatibleDC(hdcScreen);
		nint hBmpFull = Native.CreateCompatibleBitmap(hdcScreen, winW, winH);
		nint hOldFull = Native.SelectObject(hdcFull, hBmpFull);

		Native.PrintWindow(hWnd, hdcFull, 2);

		nint hdcThumb = Native.CreateCompatibleDC(hdcScreen);
		nint hBmpThumb = Native.CreateCompatibleBitmap(hdcScreen, tw, th);
		nint hOldThumb = Native.SelectObject(hdcThumb, hBmpThumb);

		Native.SetStretchBltMode(hdcThumb, 4);
		Native.StretchBlt(hdcThumb, 0, 0, tw, th, hdcFull, 0, 0, winW, winH, 0x00CC0020);

		var pixels = ExtractPixels(hdcThumb, hBmpThumb, tw, th);

		Native.SelectObject(hdcThumb, hOldThumb);
		Native.DeleteObject(hBmpThumb);
		Native.DeleteDC(hdcThumb);
		Native.SelectObject(hdcFull, hOldFull);
		Native.DeleteObject(hBmpFull);
		Native.DeleteDC(hdcFull);
		Native.ReleaseDC(nint.Zero, hdcScreen);
		return pixels;
	}

	private static byte[]? ExtractPixels(nint hdc, nint hBitmap, int w, int h)
	{
		var bi = new Native.BITMAPINFOHEADER
		{
			biSize = 40,
			biWidth = w,
			biHeight = h,
			biPlanes = 1,
			biBitCount = 32,
			biSizeImage = (uint)(w * h * 4),
		};

		var pixels = new byte[w * h * 4];
		int result = Native.GetDIBits(hdc, hBitmap, 0, (uint)h, pixels, ref bi, 0);
		if (result == 0)
			return null;

		for (int i = 3; i < pixels.Length; i += 4)
			pixels[i] = 255;

		return pixels;
	}

	private static BitmapImage? CreateThumbnail(byte[] bgraPixels, int width, int height)
	{
		try
		{
			var bmpData = EncodeBmp(bgraPixels, width, height);
			var tempPath = Path.Combine(Path.GetTempPath(), $"elapsed-{Guid.NewGuid():N}.bmp");
			File.WriteAllBytes(tempPath, bmpData);
			return new BitmapImage(new Uri(tempPath));
		}
		catch
		{
			return null;
		}
	}

	private static BitmapImage? ExtractWindowIcon(nint hWnd)
	{
		try
		{
			var hIcon = Native.SendMessageW(hWnd, Native.WM_GETICON, 1 /* ICON_BIG */, nint.Zero);
			if (hIcon == nint.Zero)
				hIcon = Native.GetClassLongPtrW(hWnd, Native.GCL_HICON);
			if (hIcon == nint.Zero)
				hIcon = Native.SendMessageW(hWnd, Native.WM_GETICON, Native.ICON_SMALL2, nint.Zero);
			if (hIcon == nint.Zero)
				hIcon = Native.GetClassLongPtrW(hWnd, Native.GCL_HICONSM);
			if (hIcon == nint.Zero)
				return null;

			if (Native.GetIconInfo(hIcon, out var iconInfo) == 0)
				return null;

			try
			{
				var colorBmp = iconInfo.hbmColor;
				if (colorBmp == nint.Zero) return null;

				Native.GetObjectW(colorBmp, Marshal.SizeOf<Native.BITMAP>(), out var bm);
				int w = bm.bmWidth;
				int h = bm.bmHeight;
				if (w <= 0 || h <= 0) return null;

				nint hdcScreen = Native.GetDC(nint.Zero);
				nint hdcMem = Native.CreateCompatibleDC(hdcScreen);
				nint hOld = Native.SelectObject(hdcMem, colorBmp);

				var bi = new Native.BITMAPINFOHEADER
				{
					biSize = 40,
					biWidth = w,
					biHeight = -h,
					biPlanes = 1,
					biBitCount = 32,
					biSizeImage = (uint)(w * h * 4),
				};

				var pixels = new byte[w * h * 4];
				Native.GetDIBits(hdcMem, colorBmp, 0, (uint)h, pixels, ref bi, 0);

				Native.SelectObject(hdcMem, hOld);
				Native.DeleteDC(hdcMem);
				Native.ReleaseDC(nint.Zero, hdcScreen);

				var info = new SKImageInfo(w, h, SKColorType.Bgra8888, SKAlphaType.Unpremul);
				using var bitmap = new SKBitmap(info);
				unsafe
				{
					fixed (byte* ptr = pixels)
						bitmap.InstallPixels(info, (nint)ptr, w * 4);

					using var image = SKImage.FromBitmap(bitmap);
					using var data = image.Encode(SKEncodedImageFormat.Png, 100);
					var path = Path.Combine(Path.GetTempPath(), $"elapsed-icon-{Guid.NewGuid():N}.png");
					using (var stream = File.OpenWrite(path))
						data.SaveTo(stream);
					return new BitmapImage(new Uri(path));
				}
			}
			finally
			{
				if (iconInfo.hbmColor != nint.Zero)
					Native.DeleteObject(iconInfo.hbmColor);
				if (iconInfo.hbmMask != nint.Zero)
					Native.DeleteObject(iconInfo.hbmMask);
			}
		}
		catch
		{
			return null;
		}
	}

	private static byte[] EncodeBmp(byte[] bgraPixels, int width, int height)
		=> BmpEncoder.Encode(bgraPixels, width, height);

	private static (int w, int h) ScaleToFit(int srcW, int srcH)
		=> ScaleToFit(srcW, srcH, ThumbMaxWidth, ThumbMaxHeight);

	private static (int w, int h) ScaleToFit(int srcW, int srcH, int maxW, int maxH)
	{
		double scale = Math.Min((double)maxW / srcW, (double)maxH / srcH);
		if (scale > 1) scale = 1;
		return (Math.Max(1, (int)(srcW * scale)), Math.Max(1, (int)(srcH * scale)));
	}

	private static class Native
	{
		public delegate bool MonitorEnumProc(nint hMonitor, nint hdcMonitor, ref RECT lprcMonitor, nint dwData);
		public delegate bool EnumWindowsProc(nint hWnd, nint lParam);

		[DllImport("user32.dll")]
		public static extern bool EnumDisplayMonitors(nint hdc, nint lprcClip, MonitorEnumProc lpfnEnum, nint dwData);

		[DllImport("user32.dll", CharSet = CharSet.Unicode)]
		public static extern bool GetMonitorInfoW(nint hMonitor, ref MONITORINFOEX lpmi);

		[DllImport("user32.dll", CharSet = CharSet.Unicode)]
		public static extern bool EnumDisplaySettingsW(string lpszDeviceName, int iModeNum, ref DEVMODE lpDevMode);

		[DllImport("user32.dll")]
		public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, nint lParam);

		[DllImport("user32.dll")]
		public static extern bool IsWindowVisible(nint hWnd);

		[DllImport("user32.dll", CharSet = CharSet.Unicode)]
		public static extern int GetWindowTextLengthW(nint hWnd);

		[DllImport("user32.dll", CharSet = CharSet.Unicode)]
		public static extern int GetWindowTextW(nint hWnd, StringBuilder lpString, int nMaxCount);

		[DllImport("user32.dll")]
		public static extern uint GetWindowThreadProcessId(nint hWnd, out uint lpdwProcessId);

		[DllImport("user32.dll")]
		public static extern bool GetWindowRect(nint hWnd, out RECT lpRect);

		[DllImport("user32.dll")]
		public static extern int GetWindowLongW(nint hWnd, int nIndex);

		[DllImport("dwmapi.dll")]
		public static extern int DwmGetWindowAttribute(nint hwnd, int dwAttribute, out int pvAttribute, int cbAttribute);

		[DllImport("user32.dll")]
		public static extern nint MonitorFromWindow(nint hwnd, uint dwFlags);

		[DllImport("user32.dll")]
		public static extern bool PrintWindow(nint hWnd, nint hdcBlt, uint nFlags);

		[DllImport("user32.dll")]
		public static extern nint GetDC(nint hWnd);

		[DllImport("user32.dll")]
		public static extern int ReleaseDC(nint hWnd, nint hDC);

		[DllImport("gdi32.dll")]
		public static extern nint CreateCompatibleDC(nint hdc);

		[DllImport("gdi32.dll")]
		public static extern nint CreateCompatibleBitmap(nint hdc, int cx, int cy);

		[DllImport("gdi32.dll")]
		public static extern nint SelectObject(nint hdc, nint h);

		[DllImport("gdi32.dll")]
		public static extern bool DeleteObject(nint ho);

		[DllImport("gdi32.dll")]
		public static extern bool DeleteDC(nint hdc);

		[DllImport("gdi32.dll")]
		public static extern int SetStretchBltMode(nint hdc, int mode);

		[DllImport("gdi32.dll")]
		public static extern bool StretchBlt(nint hdcDest, int xDest, int yDest, int wDest, int hDest, nint hdcSrc, int xSrc, int ySrc, int wSrc, int hSrc, uint rop);

		[DllImport("gdi32.dll")]
		public static extern int GetDIBits(nint hdc, nint hbm, uint start, uint cLines, byte[] lpvBits, ref BITMAPINFOHEADER lpbmi, uint usage);

		[DllImport("avicap32.dll", CharSet = CharSet.Unicode)]
		public static extern bool capGetDriverDescriptionW(uint wDriverIndex, StringBuilder lpszName, int cbName, StringBuilder lpszVer, int cbVer);

		[DllImport("user32.dll")]
		public static extern nint SendMessageW(nint hWnd, uint msg, nint wParam, nint lParam);

		[DllImport("user32.dll")]
		public static extern nint GetClassLongPtrW(nint hWnd, int nIndex);

		[DllImport("user32.dll")]
		public static extern bool DestroyIcon(nint hIcon);

		[DllImport("user32.dll")]
		public static extern int GetIconInfo(nint hIcon, out ICONINFO piconinfo);

		[DllImport("gdi32.dll")]
		public static extern int GetObjectW(nint h, int c, out BITMAP pv);

		public const uint WM_GETICON = 0x007F;
		public const nint ICON_SMALL2 = 2;
		public const int GCL_HICONSM = -34;
		public const int GCL_HICON = -14;

		[StructLayout(LayoutKind.Sequential)]
		public struct ICONINFO
		{
			public bool fIcon;
			public int xHotspot, yHotspot;
			public nint hbmMask, hbmColor;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct BITMAP
		{
			public int bmType, bmWidth, bmHeight, bmWidthBytes;
			public ushort bmPlanes, bmBitsPixel;
			public nint bmBits;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct RECT
		{
			public int left, top, right, bottom;
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		public struct MONITORINFOEX
		{
			public int cbSize;
			public RECT rcMonitor;
			public RECT rcWork;
			public uint dwFlags;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
			public string szDevice;
		}

		[StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode, Size = 220)]
		public struct DEVMODE
		{
			[FieldOffset(68)]
			public ushort dmSize;
			[FieldOffset(184)]
			public uint dmDisplayFrequency;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct BITMAPINFOHEADER
		{
			public uint biSize;
			public int biWidth;
			public int biHeight;
			public ushort biPlanes;
			public ushort biBitCount;
			public uint biCompression;
			public uint biSizeImage;
			public int biXPelsPerMeter;
			public int biYPelsPerMeter;
			public uint biClrUsed;
			public uint biClrImportant;
		}
	}
}
#endif
