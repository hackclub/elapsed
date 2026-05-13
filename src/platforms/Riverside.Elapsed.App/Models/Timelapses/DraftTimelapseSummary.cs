namespace Riverside.Elapsed.App.Models.Timelapses;

public sealed class DraftTimelapseSummary
{
	public string DraftTimelapseId { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public DateTimeOffset CreatedAt { get; set; }
	public User.User Owner { get; set; } = new();
	public Guid DeviceId { get; set; }
	public string IvHex { get; set; } = string.Empty;
	public Uri PreviewThumbnailUrl { get; set; } = new Uri("https://example.com", UriKind.Absolute);
	public IReadOnlyList<Uri> Sessions { get; set; } = Array.Empty<Uri>();
	public IReadOnlyList<DraftEdit> EditList { get; set; } = Array.Empty<DraftEdit>();
	public string? AssociatedTimelapseId { get; set; }
}
