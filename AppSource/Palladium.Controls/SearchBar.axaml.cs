using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Palladium.Controls;

public class SearchBar : TemplatedControl
{
	public static readonly StyledProperty<string?> WatermarkProperty = TextBox.WatermarkProperty.AddOwner<SearchBar>();
	public static readonly StyledProperty<string?> TextProperty = TextBox.TextProperty.AddOwner<SearchBar>();

	public string? Watermark
	{
		get => GetValue(WatermarkProperty);
		set => SetValue(WatermarkProperty, value);
	}

	public string? Text
	{
		get => GetValue(TextProperty);
		set => SetValue(TextProperty, value);
	}
}