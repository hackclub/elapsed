#if false
using Riverside.Elapsed.App.Models.Primitives;

namespace Riverside.Elapsed.App.Models.Timelapses.Local;

public sealed record LocalSession : IUploadable
{
	public Guid LocalSessionId { get; init; }

	public string FilePath { get; init; } = string.Empty;
	public long FileSizeBytes { get; init; }

	public string? UploadToken { get; init; }
	public TusUploadState Upload { get; init; } = new();
}

#endif
