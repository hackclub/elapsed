namespace Riverside.Elapsed.App.Models.User;

public class User
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

	public static User FromDetails(UserDetails details)
	{
		ArgumentNullException.ThrowIfNull(details);

		return new User
		{
			UserId = details.UserId,
			CreatedAt = details.CreatedAt,
			Handle = details.Handle,
			DisplayName = details.DisplayName,
			ProfilePictureUrl = details.ProfilePictureUrl,
			Bio = details.Bio,
			Urls = details.Urls,
			HackatimeId = details.HackatimeId,
			SlackId = details.SlackId,
		};
	}

	public static User FromSummary(UserSummary summary)
	{
		ArgumentNullException.ThrowIfNull(summary);

		return new User
		{
			UserId = summary.UserId,
			Handle = summary.Handle,
			DisplayName = summary.DisplayName,
			ProfilePictureUrl = summary.ProfilePictureUrl ?? new Uri("https://example.com", UriKind.Absolute),
			Bio = string.Empty,
			Urls = Array.Empty<Uri>(),
		};
	}

	/// <summary>
	/// Creates a fully projected user from a summary by querying the API for missing details.
	/// </summary>
	public static async Task<User?> CreateHydratedAsync(
		UserSummary? summary,
		Func<string, CancellationToken, Task<UserDetails?>> hydrateByIdAsync,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(hydrateByIdAsync);

		if (summary is null || string.IsNullOrWhiteSpace(summary.UserId))
		{
			return null;
		}

		var details = await hydrateByIdAsync(summary.UserId, cancellationToken);
		return details is not null
			? FromDetails(details)
			: FromSummary(summary);
	}
}
