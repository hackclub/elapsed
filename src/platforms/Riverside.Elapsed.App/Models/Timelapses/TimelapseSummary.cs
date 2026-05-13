namespace Riverside.Elapsed.App.Models.Timelapses;

public sealed class TimelapseSummary
{
	public string TimelapseId { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public Visibility Visibility { get; set; }
	public DateTimeOffset CreatedAt { get; set; }
	public User.User Owner { get; set; } = new();
	public Uri? PlaybackUrl { get; set; }
	public Uri? ThumbnailUrl { get; set; }
	public double DurationSeconds { get; set; }
}
