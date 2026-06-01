using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace Riverside.Elapsed.App.Converters;

public sealed class NullToCollapsedConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, string language)
	{
		return value is null or "" ? Visibility.Collapsed : Visibility.Visible;
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language)
	{
		throw new NotSupportedException();
	}
}
