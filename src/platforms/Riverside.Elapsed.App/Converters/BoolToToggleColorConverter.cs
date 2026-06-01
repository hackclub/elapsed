using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace Riverside.Elapsed.App.Converters;

public sealed class BoolToToggleColorConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, string language)
	{
		if (value is true)
		{
			return new SolidColorBrush(Microsoft.UI.Colors.DodgerBlue);
		}

		return new SolidColorBrush(Microsoft.UI.Colors.Coral);
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language)
	{
		throw new NotSupportedException();
	}
}
