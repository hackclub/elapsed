using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Riverside.Elapsed.App.Models.Recording;

public sealed class CaptureSource : INotifyPropertyChanged
{
	private string _name = "";
	private string? _description;
	private string? _resolution;
	private Microsoft.UI.Xaml.Media.ImageSource? _thumbnail;
	private Microsoft.UI.Xaml.Media.ImageSource? _icon;
	private WriteableBitmap? _thumbnailBitmap;

	public required string Id { get; init; }
	public required CaptureSourceKind Kind { get; init; }

	public required string Name
	{
		get => _name;
		set => SetField(ref _name, value);
	}

	public string? Description
	{
		get => _description;
		set => SetField(ref _description, value);
	}

	public string? Resolution
	{
		get => _resolution;
		set => SetField(ref _resolution, value);
	}

	public Microsoft.UI.Xaml.Media.ImageSource? Thumbnail
	{
		get => _thumbnail;
		set => SetField(ref _thumbnail, value);
	}

	public Microsoft.UI.Xaml.Media.ImageSource? Icon
	{
		get => _icon;
		set => SetField(ref _icon, value);
	}

	public void BlitThumbnail(byte[] bgra, int width, int height, bool bottomUp = false)
	{
		if (_thumbnailBitmap is null || _thumbnailBitmap.PixelWidth != width || _thumbnailBitmap.PixelHeight != height)
		{
			_thumbnailBitmap = new WriteableBitmap(width, height);
			Thumbnail = _thumbnailBitmap;
		}

		using var stream = _thumbnailBitmap.PixelBuffer.AsStream();
		if (bottomUp)
		{
			int stride = width * 4;
			for (int y = height - 1; y >= 0; y--)
				stream.Write(bgra, y * stride, stride);
		}
		else
		{
			stream.Write(bgra, 0, width * height * 4);
		}
		_thumbnailBitmap.Invalidate();
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	private void SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
	{
		if (EqualityComparer<T>.Default.Equals(field, value)) return;

		field = value;
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}
}
