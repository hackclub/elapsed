#if false
using System.Globalization;

namespace Riverside.Elapsed.App.Services.Build;

public sealed class BuildInfoProvider : IBuildInfoProvider
{
	public BuildInfo GetBuildInfo()
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

		return new BuildInfo
		{
			DisplayVersion = versionText,
			BuildTimestamp = timestamp,
			FullFooterText = full,
			WebFooterText = compact,
		};
	}
}

#endif
