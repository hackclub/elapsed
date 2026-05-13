namespace Riverside.Elapsed.App.Models.User;

public sealed class UserSummary
{
	public string UserId { get; set; } = string.Empty;
	public string Handle { get; set; } = string.Empty;
	public string DisplayName { get; set; } = string.Empty;
	public Uri? ProfilePictureUrl { get; set; }
}
