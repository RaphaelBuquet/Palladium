using System.Collections;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Palladium.Converters;

public class EnumerableContainsNoItemsToBoolConverter : IValueConverter
{
	public static EnumerableContainsNoItemsToBoolConverter Instance { get; } = new ();

	/// <inheritdoc />
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is IEnumerable enumerable)
		{
			return !enumerable.OfType<object?>().Any();
		}
		return true;
	}

	/// <inheritdoc />
	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}