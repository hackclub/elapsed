using CommunityToolkit.WinUI.Converters;

namespace Riverside.Elapsed.App.Converters;

public sealed partial class BoolToPausedRecordingLabelConverter : BoolToObjectConverter
{
	public BoolToPausedRecordingLabelConverter()
	{
		TrueValue = "Paused";
		FalseValue = "Recording";
	}
}
