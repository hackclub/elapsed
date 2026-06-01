using Riverside.Elapsed.App.Models.Recording;

namespace Riverside.Elapsed.App.Services.Recording;

public interface ICaptureSourceProvider
{
	Task<IReadOnlyList<CaptureSource>> GetSourcesAsync(CaptureSourceKind kind);
	Task<Microsoft.UI.Xaml.Media.ImageSource?> CapturePreviewAsync(CaptureSource source, int maxWidth, int maxHeight);
	Task<byte[]?> CapturePreviewBytesAsync(CaptureSource source, int maxWidth, int maxHeight);
	Task<CapturedFrame?> CaptureFrameAsync(CaptureSource source);
}
