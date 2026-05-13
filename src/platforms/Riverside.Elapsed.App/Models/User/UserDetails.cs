namespace Riverside.Elapsed.App.Models.User;

public sealed class UserDetails
{
	public string UserId { get; set; } = string.Empty;
	public DateTimeOffset CreatedAt { get; set; }
	public string Handle { get; set; } = string.Empty;
	public string DisplayName { get; set; } = string.Empty;
	public Uri ProfilePictureUrl { get; set; } = new Uri("https://example.com", UriKind.Absolute);
	public string Bio { get; set; } = string.Empty;
	public IReadOnlyList<Uri> Urls { get; set; } = Array.Empty<Uri>();
	public string? HackatimeId { get; set; }
	public string? SlackId { get; set; }
}
