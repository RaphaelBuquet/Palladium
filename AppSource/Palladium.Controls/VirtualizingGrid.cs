using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using ReactiveUI;

namespace Palladium.Controls;

public class VirtualizingGrid : Grid
{
	private static readonly AttachedProperty<object?> RecycleKeyProperty =
		AvaloniaProperty.RegisterAttached<VirtualizingGrid, Control, object?>("RecycleKey");

	public static readonly StyledProperty<IReadOnlyList<object>> ItemsSourceProperty =
		AvaloniaProperty.Register<ItemsControl, IReadOnlyList<object>>(nameof(ItemsSource), new List<object>());

	/// <inheritdoc />
	public VirtualizingGrid()
	{
		this.WhenAnyValue(x => x.ItemsSource)
			.Subscribe(_ =>
			{
				Children.Clear(); // they will be recreated on the next measure
			});
	}

	public IReadOnlyList<object> ItemsSource
	{
		get => GetValue(ItemsSourceProperty);
		set => SetValue(ItemsSourceProperty, value);
	}

	/// <inheritdoc />
	protected override Size MeasureOverride(Size availableSize)
	{
		RealizeElements(availableSize);
		return CalculateDesiredSize(availableSize);
	}

	private Size CalculateDesiredSize(Size availableSize)
	{
		return base.MeasureOverride(availableSize);
	}

	private void RealizeElements(Size availableSize)
	{
		if (Children.Count != 0)
		{
			return;
		}

		for (var index = 0; index < ItemsSource.Count; index++)
		{
			object item = ItemsSource[index];
			Control control = CreateElement(item);
			AddItemAsChild(control);
			control.Measure(availableSize);
		}
	}

	private Control CreateElement(object item)
	{
		IDataTemplate dataTemplate = this.FindDataTemplate(item) ?? FuncDataTemplate.Default;
		Control control = dataTemplate.Build(item) ?? throw new Exception($"Failed to create control for item \"{item}\"");
		control.DataContext = item;
		return control;
	}

	private void AddItemAsChild(Control control)
	{
		Children.Add(control);
	}
}