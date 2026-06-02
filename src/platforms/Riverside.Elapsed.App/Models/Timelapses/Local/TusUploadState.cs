#if false
namespace Riverside.Elapsed.App.Models.Timelapses.Local;

public class TusUploadState
{
	public string? UploadUrl { get; init; }

	public long BytesUploaded { get; init; }
	public long BytesTotal { get; init; }
	public bool IsComplete { get; init; }
	public string? LastError { get; init; }

	public DateTimeOffset StartedAt { get; init; }
	public DateTimeOffset? CompletedAt { get; init; }
}

#endif
