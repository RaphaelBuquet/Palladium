using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Metadata;

namespace Palladium.Controls;

public class HeadedSection : TemplatedControl
{
	public static readonly StyledProperty<Control?> ChildProperty = Decorator.ChildProperty.AddOwner<HeadedSection>();

	[Content]
	public Control? Child
	{
		get => GetValue(ChildProperty);
		set => SetValue(ChildProperty, value);
	}
}