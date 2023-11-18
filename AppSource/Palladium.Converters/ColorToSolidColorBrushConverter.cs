using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Palladium.Converters;

public class ColorToSolidColorBrushConverter : IValueConverter
{
	public static ColorToSolidColorBrushConverter Instance { get; } = new ColorToSolidColorBrushConverter();

	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is Color color)
		{
			return new SolidColorBrush(color);
		}
		return AvaloniaProperty.UnsetValue;
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}