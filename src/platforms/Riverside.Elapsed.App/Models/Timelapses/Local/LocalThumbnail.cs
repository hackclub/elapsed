#if false
using System;
using System.Collections.Generic;
using System.Text;
using Riverside.Elapsed.App.Models.Primitives;

namespace Riverside.Elapsed.App.Models.Timelapses.Local;

public sealed record LocalThumbnail : IUploadable
{
	public string FilePath { get; init; } = string.Empty;
	public long FileSizeBytes { get; init; }

	public string? UploadToken { get; init; }
	public TusUploadState Upload { get; init; } = new();
}

#endif
