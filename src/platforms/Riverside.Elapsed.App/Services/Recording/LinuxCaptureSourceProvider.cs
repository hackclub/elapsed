#if HAS_MEDIA_RECORDING
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.UI.Xaml.Media.Imaging;
using Riverside.Elapsed.App.Models.Recording;

namespace Riverside.Elapsed.App.Services.Recording;

public sealed class LinuxCaptureSourceProvider : ICaptureSourceProvider
{
	private const int ZPixmap = 2;
	private const ulong AllPlanes = ~0UL;

	public async Task<IReadOnlyList<CaptureSource>> GetSourcesAsync(CaptureSourceKind kind)
	{
		return kind switch
		{
			CaptureSourceKind.Screen => EnumerateScreens(),
			CaptureSourceKind.Window => EnumerateWindows(),
			CaptureSourceKind.Camera => await EnumerateCamerasAsync(),
			_ => []
		};
	}

	public async Task<Microsoft.UI.Xaml.Media.ImageSource?> CapturePreviewAsync(CaptureSource source, int maxWidth, int maxHeight)
	{
		var frame = await Task.Run(() => CaptureFrameCore(source));
		if (frame is null) return null;

		var bmpData = EncodeBmp(frame.Pixels, frame.Width, frame.Height);
		var path = Path.Combine(Path.GetTempPath(), $"elapsed-preview-{Guid.NewGuid():N}.bmp");
		File.WriteAllBytes(path, bmpData);
		return new BitmapImage(new Uri(path));
	}

	public Task<byte[]?> CapturePreviewBytesAsync(CaptureSource source, int maxWidth, int maxHeight)
	{
		return Task.Run(() =>
		{
			var frame = CaptureFrameCore(source);
			if (frame is null) return null;
			return (byte[]?)EncodeBmp(frame.Pixels, frame.Width, frame.Height);
		});
	}

	public Task<CapturedFrame?> CaptureFrameAsync(CaptureSource source)
	{
		return Task.Run(() => CaptureFrameCore(source));
	}

	private static CapturedFrame? CaptureFrameCore(CaptureSource source)
	{
		var display = X11.XOpenDisplay(null);
		if (display == nint.Zero) return null;

		try
		{
			if (source.Kind == CaptureSourceKind.Screen)
			{
				if (!int.TryParse(source.Id.Replace("screen-", ""), out int screenIndex))
					return null;

				var root = X11.XRootWindow(display, screenIndex);
				int w = X11.XDisplayWidth(display, screenIndex);
				int h = X11.XDisplayHeight(display, screenIndex);
				if (w <= 0 || h <= 0) return null;

				return CaptureDrawable(display, root, w, h);
			}
			else if (source.Kind == CaptureSourceKind.Window)
			{
				if (!nint.TryParse(source.Id.Replace("window-", ""), out var windowId))
					return null;

				X11.XGetWindowAttributes(display, windowId, out var attrs);
				if (attrs.width <= 0 || attrs.height <= 0) return null;

				return CaptureDrawable(display, windowId, attrs.width, attrs.height);
			}

			return null;
		}
		finally
		{
			X11.XCloseDisplay(display);
		}
	}

	private static CapturedFrame? CaptureDrawable(nint display, nint drawable, int width, int height)
	{
		var xImage = X11.XGetImage(display, drawable, 0, 0, (uint)width, (uint)height, AllPlanes, ZPixmap);
		if (xImage == nint.Zero) return null;

		try
		{
			var imageInfo = Marshal.PtrToStructure<XImage>(xImage);
			if (imageInfo.data == nint.Zero || imageInfo.bits_per_pixel != 32)
				return null;

			int stride = imageInfo.bytes_per_line;
			var pixels = new byte[height * width * 4];

			for (int y = 0; y < height; y++)
			{
				Marshal.Copy(imageInfo.data + y * stride, pixels, y * width * 4, width * 4);
			}

			return new CapturedFrame(pixels, width, height);
		}
		finally
		{
			X11.XDestroyImage(xImage);
		}
	}

	private static List<CaptureSource> EnumerateScreens()
	{
		var results = new List<CaptureSource>();
		var display = X11.XOpenDisplay(null);
		if (display == nint.Zero) return results;

		try
		{
			int screenCount = X11.XScreenCount(display);
			for (int i = 0; i < screenCount; i++)
			{
				int w = X11.XDisplayWidth(display, i);
				int h = X11.XDisplayHeight(display, i);

				results.Add(new CaptureSource
				{
					Id = $"screen-{i}",
					Name = i == 0 ? "Primary Display" : $"Display {i + 1}",
					Resolution = $"{w}x{h}",
					Kind = CaptureSourceKind.Screen,
				});
			}
		}
		finally
		{
			X11.XCloseDisplay(display);
		}

		return results;
	}

	private static List<CaptureSource> EnumerateWindows()
	{
		var results = new List<CaptureSource>();
		var display = X11.XOpenDisplay(null);
		if (display == nint.Zero) return results;

		try
		{
			int ownPid = Environment.ProcessId;
			var root = X11.XRootWindow(display, 0);
			var netWmName = X11.XInternAtom(display, "_NET_WM_NAME", false);
			var utf8String = X11.XInternAtom(display, "UTF8_STRING", false);
			var netWmPid = X11.XInternAtom(display, "_NET_WM_PID", false);

			if (X11.XQueryTree(display, root, out _, out _, out var childrenPtr, out uint nChildren) == 0)
				return results;

			if (childrenPtr == nint.Zero) return results;

			try
			{
				for (int i = 0; i < (int)nChildren; i++)
				{
					var child = Marshal.ReadIntPtr(childrenPtr + i * nint.Size);

					X11.XGetWindowAttributes(display, child, out var attrs);
					if (attrs.map_state != 2) continue;
					if (attrs.width <= 1 || attrs.height <= 1) continue;

					int windowPid = GetWindowPid(display, child, netWmPid);
					if (windowPid == ownPid) continue;

					string? name = GetWindowName(display, child, netWmName, utf8String);
					if (string.IsNullOrWhiteSpace(name))
					{
						X11.XFetchName(display, child, out var namePtr);
						if (namePtr != nint.Zero)
						{
							name = Marshal.PtrToStringUTF8(namePtr);
							X11.XFree(namePtr);
						}
					}

					if (string.IsNullOrWhiteSpace(name)) continue;

					results.Add(new CaptureSource
					{
						Id = $"window-{child}",
						Name = name!,
						Resolution = $"{attrs.width}x{attrs.height}",
						Kind = CaptureSourceKind.Window,
					});
				}
			}
			finally
			{
				X11.XFree(childrenPtr);
			}
		}
		finally
		{
			X11.XCloseDisplay(display);
		}

		return results;
	}

	private static string? GetWindowName(nint display, nint window, nint netWmName, nint utf8String)
	{
		int result = X11.XGetWindowProperty(display, window, netWmName, 0, 1024, false, utf8String,
			out _, out int actualFormat, out nuint nItems, out _, out var prop);

		if (result != 0 || prop == nint.Zero || nItems == 0)
			return null;

		try
		{
			return Marshal.PtrToStringUTF8(prop);
		}
		finally
		{
			X11.XFree(prop);
		}
	}

	private static int GetWindowPid(nint display, nint window, nint netWmPid)
	{
		int result = X11.XGetWindowProperty(display, window, netWmPid, 0, 1, false, nint.Zero,
			out _, out int format, out nuint nItems, out _, out var prop);

		if (result != 0 || prop == nint.Zero || nItems == 0)
			return 0;

		try
		{
			return Marshal.ReadInt32(prop);
		}
		finally
		{
			X11.XFree(prop);
		}
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

						source.Thumbnail = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(thumbPath));
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

	private static byte[] EncodeBmp(byte[] bgraPixels, int width, int height)
		=> BmpEncoder.Encode(bgraPixels, width, height, topDown: true);

	[StructLayout(LayoutKind.Sequential)]
	private struct XImage
	{
		public int width, height;
		public int xoffset;
		public int format;
		public nint data;
		public int byte_order;
		public int bitmap_unit;
		public int bitmap_bit_order;
		public int bitmap_pad;
		public int depth;
		public int bytes_per_line;
		public int bits_per_pixel;
		public ulong red_mask, green_mask, blue_mask;
		public nint obdata;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct XWindowAttributes
	{
		public int x, y;
		public int width, height;
		public int border_width;
		public int depth;
		public nint visual;
		public nint root;
		public int @class;
		public int bit_gravity;
		public int win_gravity;
		public int backing_store;
		public ulong backing_planes;
		public ulong backing_pixel;
		public int save_under;
		public nint colormap;
		public int map_installed;
		public int map_state;
		public long all_event_masks;
		public long your_event_mask;
		public long do_not_propagate_mask;
		public int override_redirect;
		public nint screen;
	}

	private static class X11
	{
		const string Lib = "libX11.so.6";

		[DllImport(Lib)]
		public static extern nint XOpenDisplay(string? displayName);

		[DllImport(Lib)]
		public static extern int XCloseDisplay(nint display);

		[DllImport(Lib)]
		public static extern nint XRootWindow(nint display, int screenNumber);

		[DllImport(Lib)]
		public static extern int XScreenCount(nint display);

		[DllImport(Lib)]
		public static extern int XDisplayWidth(nint display, int screenNumber);

		[DllImport(Lib)]
		public static extern int XDisplayHeight(nint display, int screenNumber);

		[DllImport(Lib)]
		public static extern nint XGetImage(nint display, nint drawable, int x, int y, uint width, uint height, ulong planeMask, int format);

		[DllImport(Lib)]
		public static extern int XDestroyImage(nint ximage);

		[DllImport(Lib)]
		public static extern int XQueryTree(nint display, nint window, out nint rootReturn, out nint parentReturn, out nint childrenReturn, out uint nChildrenReturn);

		[DllImport(Lib)]
		public static extern int XFree(nint data);

		[DllImport(Lib)]
		public static extern int XGetWindowAttributes(nint display, nint window, out XWindowAttributes attributes);

		[DllImport(Lib)]
		public static extern int XFetchName(nint display, nint window, out nint name);

		[DllImport(Lib)]
		public static extern int XGetWindowProperty(nint display, nint window, nint property, long offset, long length, bool delete, nint reqType, out nint actualType, out int actualFormat, out nuint nItems, out nuint bytesAfter, out nint prop);

		[DllImport(Lib)]
		public static extern nint XInternAtom(nint display, [MarshalAs(UnmanagedType.LPUTF8Str)] string name, bool onlyIfExists);
	}
}
#endif
