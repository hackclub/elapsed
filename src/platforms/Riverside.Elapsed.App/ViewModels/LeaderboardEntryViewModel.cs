using Riverside.Elapsed.App.Models.Global;

namespace Riverside.Elapsed.App.ViewModels;

/// <summary>
/// View-model for a single leaderboard avatar tile (used by <c>LeaderboardControl</c>).
/// </summary>
public sealed class LeaderboardEntryViewModel
{
	/// <summary>Gets the user's display name.</summary>
	public string Name { get; init; } = string.Empty;

	/// <summary>Gets the user's <c>@handle</c> with leading at-sign, or empty.</summary>
	public string Handle { get; init; } = string.Empty;

	/// <summary>Gets the underlying user identifier (used for profile navigation).</summary>
	public string UserId { get; init; } = string.Empty;

	/// <summary>Gets the formatted weekly recording duration (e.g. "28h 20m recorded this week").</summary>
	public string WeeklyText { get; init; } = string.Empty;

	/// <summary>Gets the URL of the user's profile picture.</summary>
	public Uri? ProfilePictureUrl { get; init; }

	/// <summary>Creates an entry view-model from the raw <see cref="LeaderboardEntry"/> response.</summary>
	public static LeaderboardEntryViewModel FromModel(LeaderboardEntry entry)
	{
		var seconds = entry.SecondsThisWeek;
		var hours = (int)(seconds / 3600);
		var minutes = (int)((seconds % 3600) / 60);
		return new LeaderboardEntryViewModel
		{
			UserId = entry.User.UserId,
			Name = entry.User.DisplayName,
			Handle = string.IsNullOrWhiteSpace(entry.User.Handle) ? string.Empty : $"@{entry.User.Handle}",
			WeeklyText = $"{hours}h {minutes}m recorded this week",
			ProfilePictureUrl = entry.User.ProfilePictureUrl,
		};
	}
}
