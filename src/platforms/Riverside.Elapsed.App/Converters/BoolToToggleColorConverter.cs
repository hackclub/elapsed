using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace Riverside.Elapsed.App.Converters;

public sealed class BoolToToggleColorConverter : IValueConverter
{
	private static readonly SolidColorBrush TrueBrush = new(Microsoft.UI.Colors.DodgerBlue);
	private static readonly SolidColorBrush FalseBrush = new(Microsoft.UI.Colors.Coral);

	public object Convert(object value, Type targetType, object parameter, string language)
		=> value is true ? TrueBrush : FalseBrush;

	public object ConvertBack(object value, Type targetType, object parameter, string language)
	{
		throw new NotSupportedException();
	}
}
