using Riverside.Elapsed.App.Models.Timelapses;
using TimelapseModel = Riverside.Elapsed.App.Models.Timelapses.Timelapse;

namespace Riverside.Elapsed.App.ViewModels;

/// <summary>
/// View-model for a single timelapse card used by the explore grid, the video page right-rail,
/// and the user profile grid.
/// </summary>
public sealed class TimelapseCardViewModel
{
	/// <summary>Gets the unique timelapse identifier.</summary>
	public string TimelapseId { get; init; } = string.Empty;

	/// <summary>Gets the timelapse title.</summary>
	public string Title { get; init; } = string.Empty;

	/// <summary>Gets the timelapse description.</summary>
	public string Description { get; init; } = string.Empty;

	/// <summary>Gets the owner's identifier (used for profile navigation).</summary>
	public string OwnerUserId { get; init; } = string.Empty;

	/// <summary>Gets the owner display + handle text (e.g. "Hack Club · @hackclub").</summary>
	public string OwnerText { get; init; } = string.Empty;

	/// <summary>Gets the formatted "13 days ago · Fallout for desktop" meta line.</summary>
	public string MetaText { get; init; } = string.Empty;

	/// <summary>Gets the URL of the thumbnail image.</summary>
	public Uri? ThumbnailUrl { get; init; }

	/// <summary>Gets the URL of the playable media.</summary>
	public Uri? PlaybackUrl { get; init; }

	/// <summary>Maps a <see cref="TimelapseModel"/> domain model into a display-ready card view-model.</summary>
	public static TimelapseCardViewModel FromModel(TimelapseModel timelapse)
	{
		ArgumentNullException.ThrowIfNull(timelapse);

		var ageText = FormatAge(DateTimeOffset.Now - timelapse.CreatedAt);
		var device = string.IsNullOrWhiteSpace(timelapse.HackatimeProject)
			? "Elapsed"
			: timelapse.HackatimeProject;

		return new TimelapseCardViewModel
		{
			TimelapseId = timelapse.TimelapseId,
			Title = timelapse.Name,
			Description = timelapse.Description,
			OwnerUserId = timelapse.Owner.UserId,
			OwnerText = $"{timelapse.Owner.DisplayName} · @{timelapse.Owner.Handle}",
			MetaText = $"{ageText} · {device}",
			ThumbnailUrl = timelapse.ThumbnailUrl,
			PlaybackUrl = timelapse.PlaybackUrl,
		};
	}

	private static string FormatAge(TimeSpan age)
	{
		if (age.TotalDays >= 30)
		{
			var months = Math.Max(1, (int)(age.TotalDays / 30));
			return months == 1 ? "1 month ago" : $"{months} months ago";
		}

		if (age.TotalDays >= 7)
		{
			var weeks = Math.Max(1, (int)(age.TotalDays / 7));
			return weeks == 1 ? "1 week ago" : $"{weeks} weeks ago";
		}

		if (age.TotalDays >= 1)
		{
			var days = (int)age.TotalDays;
			return days == 1 ? "1 day ago" : $"{days} days ago";
		}

		if (age.TotalHours >= 1)
		{
			var hours = (int)age.TotalHours;
			return hours == 1 ? "1 hour ago" : $"{hours} hours ago";
		}

		var minutes = Math.Max(1, (int)age.TotalMinutes);
		return minutes == 1 ? "1 minute ago" : $"{minutes} minutes ago";
	}
}
