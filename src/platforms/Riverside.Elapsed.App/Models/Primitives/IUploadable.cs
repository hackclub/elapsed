#if false
using Riverside.Elapsed.App.Models.Timelapses.Local;

namespace Riverside.Elapsed.App.Models.Primitives;

/// <summary>
/// Represents a locally-stored file that can be uploaded to the Lapse server online.
/// </summary>
public interface IUploadable
{
	string FilePath { get; init; }
	long FileSizeBytes { get; init; }

	string? UploadToken { get; init; }
	TusUploadState Upload { get; init; }
}

#endif
