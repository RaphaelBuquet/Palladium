using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Palladium.Controls;

public class SearchBar : TemplatedControl
{
	public static StyledProperty<string?> WatermarkProperty = TextBox.WatermarkProperty.AddOwner<SearchBar>();

	public string? Watermark
	{
		get => GetValue(WatermarkProperty);
		set => SetValue(WatermarkProperty, value);
	}
	
}