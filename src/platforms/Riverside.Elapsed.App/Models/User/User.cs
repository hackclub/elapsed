#if false
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

	/// <summary>
	/// hydrates a partially-populated user by querying the api for the full projection,
	/// falling back to the supplied user when the api lookup fails.
	/// </summary>
	public static async Task<User?> CreateHydratedAsync(
		User? user,
		Func<string, CancellationToken, Task<UserDetails?>> hydrateByIdAsync,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(hydrateByIdAsync);

		if (user is null || string.IsNullOrWhiteSpace(user.UserId))
		{
			return null;
		}

		var details = await hydrateByIdAsync(user.UserId, cancellationToken);
		return details is not null ? FromDetails(details) : user;
	}
}

#endif
