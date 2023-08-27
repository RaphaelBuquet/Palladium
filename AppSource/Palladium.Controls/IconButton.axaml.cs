using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Metadata;

namespace Palladium.Controls;

/// <summary>
///     A button that is made up of a button and text.
/// </summary>
public class IconButton : Button
{
	public static readonly StyledProperty<string?> TextProperty = TextBlock.TextProperty.AddOwner<IconButton>();
	public static readonly StyledProperty<Geometry> IconDataProperty = PathIcon.DataProperty.AddOwner<IconButton>();

	public Geometry IconData
	{
		get => GetValue(IconDataProperty);
		set => SetValue(IconDataProperty, value);
	}
}