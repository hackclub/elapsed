using Microsoft.UI.Xaml.Data;

namespace Riverside.Elapsed.App.Converters;

public sealed partial class NullToCollapsedConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, string language)
	{
		bool isNull = value is null or "";
		if (parameter is "Invert")
			isNull = !isNull;
		return isNull ? Visibility.Collapsed : Visibility.Visible;
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language)
	{
		throw new NotSupportedException();
	}
}
