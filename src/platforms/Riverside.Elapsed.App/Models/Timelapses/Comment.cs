namespace Riverside.Elapsed.App.Models.Timelapses;

public sealed class Comment
{
	public string CommentId { get; set; } = string.Empty;
	public string Content { get; set; } = string.Empty;
	public User.User Author { get; set; } = new();
	public DateTimeOffset CreatedAt { get; set; }
}
