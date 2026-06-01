#if false
using System.Globalization;

namespace Riverside.Elapsed.App.Services.Build;

public sealed class BuildInfo
{
	public string DisplayVersion { get; init; } = string.Empty;
	public DateTimeOffset BuildTimestamp { get; init; }
	public string FullFooterText { get; init; } = string.Empty;
	public string WebFooterText { get; init; } = string.Empty;

	public BuildInfo()
	{
		var timestamp = DateTimeOffset.TryParse(
			Constants.BuildTimestampIso,
			CultureInfo.InvariantCulture,
			DateTimeStyles.RoundtripKind,
			out var parsed)
			? parsed.ToLocalTime()
			: DateTimeOffset.Now;

		var timeText = timestamp.ToString("MMMM d, yyyy 'at' h:mm tt", CultureInfo.InvariantCulture).Replace("AM", "am", StringComparison.Ordinal).Replace("PM", "pm", StringComparison.Ordinal);

		var versionText = Constants.DisplayVersion;
		var full = $"A Hack Club production. Version {versionText} from {timeText}. Built with <3 by ascpixi and Lamparter.";
		var compact = $"A Hack Club production. Version {versionText}";

		DisplayVersion = versionText;
		BuildTimestamp = timestamp;
		FullFooterText = full;
		WebFooterText = compact;
	}
}

#endif
