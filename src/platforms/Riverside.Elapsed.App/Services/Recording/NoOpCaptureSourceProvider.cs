using Riverside.Elapsed.App.Models.Recording;

namespace Riverside.Elapsed.App.Services.Recording;

public sealed class NoOpCaptureSourceProvider : ICaptureSourceProvider
{
	public Task<IReadOnlyList<CaptureSource>> GetSourcesAsync(CaptureSourceKind kind)
		=> Task.FromResult<IReadOnlyList<CaptureSource>>([]);

	public Task<Microsoft.UI.Xaml.Media.ImageSource?> CapturePreviewAsync(CaptureSource source, int maxWidth, int maxHeight)
		=> Task.FromResult<Microsoft.UI.Xaml.Media.ImageSource?>(null);

	public Task<byte[]?> CapturePreviewBytesAsync(CaptureSource source, int maxWidth, int maxHeight)
		=> Task.FromResult<byte[]?>(null);

	public Task<CapturedFrame?> CaptureFrameAsync(CaptureSource source)
		=> Task.FromResult<CapturedFrame?>(null);

	public Task RefreshThumbnailAsync(CaptureSource source, int maxWidth, int maxHeight)
		=> Task.CompletedTask;
}
