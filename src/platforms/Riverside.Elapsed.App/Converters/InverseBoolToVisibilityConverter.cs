using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace Riverside.Elapsed.App.Converters;

public sealed class InverseBoolToVisibilityConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, string language)
	{
		return value is true ? Visibility.Collapsed : Visibility.Visible;
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language)
	{
		return value is Visibility visibility && visibility != Visibility.Visible;
	}
}
