using CommunityToolkit.WinUI.Converters;
using Microsoft.UI;

namespace Riverside.Elapsed.App.Converters;

public sealed class BoolToToggleColorConverter : BoolToObjectConverter
{
	public BoolToToggleColorConverter()
	{
		TrueValue = new SolidColorBrush(Colors.DodgerBlue); ;
		FalseValue = new SolidColorBrush(Colors.Coral);
	}
}
