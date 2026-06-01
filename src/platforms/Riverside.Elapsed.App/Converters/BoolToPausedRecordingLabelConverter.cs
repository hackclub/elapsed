using Microsoft.UI.Xaml.Data;

namespace Riverside.Elapsed.App.Converters;

public sealed class BoolToPausedRecordingLabelConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, string language)
	{
		return value is true ? "Paused" : "Recording";
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language)
	{
		throw new NotSupportedException();
	}
}
