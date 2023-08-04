using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace Palladium.Controls;

/// <summary>
///     A button that is made up of a button and text.
/// </summary>
[TemplatePart(Name = "PART_Button", Type = typeof(Button))]
public class IconButton : TemplatedControl
{
	public static readonly StyledProperty<string?> TextProperty = TextBlock.TextProperty.AddOwner<IconButton>();
	public static readonly StyledProperty<Geometry> IconDataProperty = PathIcon.DataProperty.AddOwner<IconButton>();

	public Button? Button { get; private set; }

	public Geometry IconData
	{
		get => GetValue(IconDataProperty);
		set => SetValue(IconDataProperty, value);
	}

	public string? Text
	{
		get => GetValue(TextProperty);
		set => SetValue(TextProperty, value);
	}

	/// <inheritdoc />
	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		base.OnApplyTemplate(e);
		Button = e.NameScope.Get<Button>("PART_Button");
	}
}