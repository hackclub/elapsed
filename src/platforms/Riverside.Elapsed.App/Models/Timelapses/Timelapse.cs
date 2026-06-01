#if false
namespace Riverside.Elapsed.App.Models.Timelapses;

public sealed class Timelapse
{
	public string TimelapseId { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public Visibility Visibility { get; set; }
	public DateTimeOffset CreatedAt { get; set; }
	public User.User Owner { get; set; } = new();
	public IReadOnlyList<Comment> Comments { get; set; } = Array.Empty<Comment>();
	public Uri? PlaybackUrl { get; set; }
	public Uri? ThumbnailUrl { get; set; }
	public double DurationSeconds { get; set; }
	public string? HackatimeProject { get; set; }
	public string? SourceDraftId { get; set; }
}

#endif
