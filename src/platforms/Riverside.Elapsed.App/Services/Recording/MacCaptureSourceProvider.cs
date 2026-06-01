#if HAS_MEDIA_RECORDING
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml.Media.Imaging;
using Riverside.Elapsed.App.Models.Recording;

namespace Riverside.Elapsed.App.Services.Recording;

public sealed class MacCaptureSourceProvider : ICaptureSourceProvider
{
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
		if (source.Kind == CaptureSourceKind.Screen)
		{
			if (!uint.TryParse(source.Id.Replace("screen-", ""), out uint displayId))
				return null;

			var imageRef = CG.CGDisplayCreateImage(displayId);
			if (imageRef == nint.Zero) return null;

			try
			{
				return ExtractPixelsFromCGImage(imageRef);
			}
			finally
			{
				CF.CFRelease(imageRef);
			}
		}
		else if (source.Kind == CaptureSourceKind.Window)
		{
			if (!uint.TryParse(source.Id.Replace("window-", ""), out uint windowId))
				return null;

			var imageRef = CG.CGWindowListCreateImage(
				CGRect.Null,
				CGWindowListOption.IncludingWindow,
				windowId,
				CGWindowImageOption.BoundsIgnoreFraming);

			if (imageRef == nint.Zero) return null;

			try
			{
				return ExtractPixelsFromCGImage(imageRef);
			}
			finally
			{
				CF.CFRelease(imageRef);
			}
		}

		return null;
	}

	private static CapturedFrame? ExtractPixelsFromCGImage(nint cgImage)
	{
		int width = (int)CG.CGImageGetWidth(cgImage);
		int height = (int)CG.CGImageGetHeight(cgImage);
		if (width == 0 || height == 0) return null;

		var dataProvider = CG.CGImageGetDataProvider(cgImage);
		if (dataProvider == nint.Zero) return null;

		var cfData = CG.CGDataProviderCopyData(dataProvider);
		if (cfData == nint.Zero) return null;

		try
		{
			var ptr = CF.CFDataGetBytePtr(cfData);
			var length = (int)CF.CFDataGetLength(cfData);

			var bitmapInfo = CG.CGImageGetBitmapInfo(cgImage);
			int bpp = (int)CG.CGImageGetBitsPerPixel(cgImage);

			if (bpp != 32) return null;

			var pixels = new byte[length];
			Marshal.Copy(ptr, pixels, 0, length);

			var alphaInfo = (CGImageAlphaInfo)(bitmapInfo & 0x1F);
			var byteOrder = bitmapInfo & (uint)CGBitmapInfo.ByteOrderMask;

			bool needsSwizzle = byteOrder == (uint)CGBitmapInfo.ByteOrder32Big ||
				alphaInfo == CGImageAlphaInfo.PremultipliedFirst ||
				alphaInfo == CGImageAlphaInfo.First ||
				alphaInfo == CGImageAlphaInfo.NoneSkipFirst;

			if (needsSwizzle)
			{
				for (int i = 0; i < pixels.Length; i += 4)
				{
					(pixels[i], pixels[i + 1], pixels[i + 2], pixels[i + 3]) =
						(pixels[i + 2], pixels[i + 1], pixels[i], pixels[i + 3]);
				}
			}

			return new CapturedFrame(pixels, width, height);
		}
		finally
		{
			CF.CFRelease(cfData);
		}
	}

	private static List<CaptureSource> EnumerateScreens()
	{
		var results = new List<CaptureSource>();
		var displays = new uint[16];
		CG.CGGetActiveDisplayList(16, displays, out uint count);

		for (int i = 0; i < (int)count; i++)
		{
			var bounds = CG.CGDisplayBounds(displays[i]);
			int w = (int)bounds.width;
			int h = (int)bounds.height;
			bool isPrimary = CG.CGDisplayIsMain(displays[i]) != 0;

			results.Add(new CaptureSource
			{
				Id = $"screen-{displays[i]}",
				Name = isPrimary ? "Primary Display" : $"Display {i + 1}",
				Resolution = $"{w}x{h}",
				Kind = CaptureSourceKind.Screen,
			});
		}

		return results;
	}

	private static List<CaptureSource> EnumerateWindows()
	{
		var results = new List<CaptureSource>();
		int ownPid = Environment.ProcessId;

		var windowList = CG.CGWindowListCopyWindowInfo(
			CGWindowListOption.OnScreenOnly | CGWindowListOption.ExcludeDesktopElements, 0);

		if (windowList == nint.Zero) return results;

		try
		{
			int count = (int)CF.CFArrayGetCount(windowList);
			for (int i = 0; i < count; i++)
			{
				var dict = CF.CFArrayGetValueAtIndex(windowList, i);
				if (dict == nint.Zero) continue;

				int layer = GetCFDictInt(dict, "kCGWindowLayer");
				if (layer != 0) continue;

				int pid = GetCFDictInt(dict, "kCGWindowOwnerPID");
				if (pid == ownPid) continue;

				string? name = GetCFDictString(dict, "kCGWindowName");
				string? owner = GetCFDictString(dict, "kCGWindowOwnerName");
				int windowId = GetCFDictInt(dict, "kCGWindowNumber");

				if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(owner)) continue;
				if (windowId == 0) continue;

				var bounds = GetCFDictBounds(dict, "kCGWindowBounds");
				string resolution = bounds.HasValue
					? $"{(int)bounds.Value.width}x{(int)bounds.Value.height}"
					: "";

				results.Add(new CaptureSource
				{
					Id = $"window-{windowId}",
					Name = string.IsNullOrEmpty(name) ? (owner ?? "Unknown") : name,
					Description = owner,
					Resolution = resolution,
					Kind = CaptureSourceKind.Window,
				});
			}
		}
		finally
		{
			CF.CFRelease(windowList);
		}

		return results;
	}

	private static async Task<List<CaptureSource>> EnumerateCamerasAsync()
	{
		try
		{
			var cameras = await FFmpegService.EnumerateCamerasAsync();
			return cameras.ConvertAll(c => new CaptureSource
			{
				Id = c.id,
				Name = c.name,
				Kind = CaptureSourceKind.Camera,
			});
		}
		catch
		{
			return [];
		}
	}

	private static int GetCFDictInt(nint dict, string key)
	{
		using var keyStr = new CFString(key);
		var value = CF.CFDictionaryGetValue(dict, keyStr.Handle);
		if (value == nint.Zero) return 0;
		CF.CFNumberGetValue(value, 3 /* kCFNumberSInt32Type */, out int result);
		return result;
	}

	private static string? GetCFDictString(nint dict, string key)
	{
		using var keyStr = new CFString(key);
		var value = CF.CFDictionaryGetValue(dict, keyStr.Handle);
		if (value == nint.Zero) return null;

		var length = CF.CFStringGetLength(value);
		if (length == 0) return "";

		var buf = CF.CFStringGetCStringPtr(value, 0x08000100 /* kCFStringEncodingUTF8 */);
		if (buf != nint.Zero)
			return Marshal.PtrToStringUTF8(buf);

		var buffer = new byte[(int)length * 4 + 1];
		unsafe
		{
			fixed (byte* p = buffer)
			{
				if (CF.CFStringGetCString(value, (nint)p, buffer.Length, 0x08000100))
					return Marshal.PtrToStringUTF8((nint)p);
			}
		}
		return null;
	}

	private static CGRect? GetCFDictBounds(nint dict, string key)
	{
		using var keyStr = new CFString(key);
		var value = CF.CFDictionaryGetValue(dict, keyStr.Handle);
		if (value == nint.Zero) return null;

		if (CG.CGRectMakeWithDictionaryRepresentation(value, out var rect))
			return rect;
		return null;
	}

	private static byte[] EncodeBmp(byte[] bgraPixels, int width, int height)
		=> BmpEncoder.Encode(bgraPixels, width, height, topDown: true);

	private sealed class CFString : IDisposable
	{
		public nint Handle { get; }

		public CFString(string value)
		{
			Handle = CF.CFStringCreateWithCString(nint.Zero, value, 0x08000100);
		}

		public void Dispose()
		{
			if (Handle != nint.Zero)
				CF.CFRelease(Handle);
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct CGRect
	{
		public double x, y, width, height;
		public static CGRect Null => new() { x = double.PositiveInfinity, y = double.PositiveInfinity, width = 0, height = 0 };
	}

	private enum CGWindowListOption : uint
	{
		All = 0,
		OnScreenOnly = 1 << 0,
		OnScreenAboveWindow = 1 << 1,
		OnScreenBelowWindow = 1 << 2,
		IncludingWindow = 1 << 3,
		ExcludeDesktopElements = 1 << 4,
	}

	private enum CGWindowImageOption : uint
	{
		Default = 0,
		BoundsIgnoreFraming = 1 << 0,
	}

	private enum CGImageAlphaInfo : uint
	{
		None = 0,
		PremultipliedLast = 1,
		PremultipliedFirst = 2,
		Last = 3,
		First = 4,
		NoneSkipLast = 5,
		NoneSkipFirst = 6,
	}

	private enum CGBitmapInfo : uint
	{
		ByteOrderMask = 0x7000,
		ByteOrder32Big = 2 << 12,
		ByteOrder32Little = 4 << 12,
	}

	private static class CG
	{
		const string Lib = "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";

		[DllImport(Lib)]
		public static extern uint CGGetActiveDisplayList(uint maxDisplays, [Out] uint[] activeDisplays, out uint displayCount);

		[DllImport(Lib)]
		public static extern nint CGDisplayCreateImage(uint displayId);

		[DllImport(Lib)]
		public static extern uint CGDisplayIsMain(uint display);

		[DllImport(Lib)]
		public static extern CGRect CGDisplayBounds(uint display);

		[DllImport(Lib)]
		public static extern nint CGWindowListCreateImage(CGRect screenBounds, CGWindowListOption listOption, uint windowId, CGWindowImageOption imageOption);

		[DllImport(Lib)]
		public static extern nint CGWindowListCopyWindowInfo(CGWindowListOption option, uint relativeToWindow);

		[DllImport(Lib)]
		public static extern nuint CGImageGetWidth(nint image);

		[DllImport(Lib)]
		public static extern nuint CGImageGetHeight(nint image);

		[DllImport(Lib)]
		public static extern uint CGImageGetBitmapInfo(nint image);

		[DllImport(Lib)]
		public static extern nuint CGImageGetBitsPerPixel(nint image);

		[DllImport(Lib)]
		public static extern nint CGImageGetDataProvider(nint image);

		[DllImport(Lib)]
		public static extern nint CGDataProviderCopyData(nint provider);

		[DllImport(Lib)]
		public static extern bool CGRectMakeWithDictionaryRepresentation(nint dict, out CGRect rect);
	}

	private static class CF
	{
		const string Lib = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";

		[DllImport(Lib)]
		public static extern void CFRelease(nint cf);

		[DllImport(Lib)]
		public static extern nint CFDataGetBytePtr(nint cfData);

		[DllImport(Lib)]
		public static extern nint CFDataGetLength(nint cfData);

		[DllImport(Lib)]
		public static extern nint CFArrayGetCount(nint array);

		[DllImport(Lib)]
		public static extern nint CFArrayGetValueAtIndex(nint array, nint index);

		[DllImport(Lib)]
		public static extern nint CFDictionaryGetValue(nint dict, nint key);

		[DllImport(Lib)]
		public static extern bool CFNumberGetValue(nint number, int theType, out int value);

		[DllImport(Lib)]
		public static extern nint CFStringGetLength(nint str);

		[DllImport(Lib)]
		public static extern nint CFStringGetCStringPtr(nint str, uint encoding);

		[DllImport(Lib)]
		public static extern bool CFStringGetCString(nint str, nint buffer, int bufferSize, uint encoding);

		[DllImport(Lib)]
		public static extern nint CFStringCreateWithCString(nint alloc, [MarshalAs(UnmanagedType.LPUTF8Str)] string cStr, uint encoding);
	}
}
#endif
