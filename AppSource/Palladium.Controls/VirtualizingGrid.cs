using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Generators;
using Avalonia.Input;

namespace Palladium.Controls;

// Currently this inherits VirtualizingPanel. But perhaps in the future it could inherit Grid, 
// and add functionality similar to ItemsControl, like ItemsSource and support for the DataTemplates.
public class VirtualizingGrid : VirtualizingPanel
{
	private static readonly AttachedProperty<object?> RecycleKeyProperty =
		AvaloniaProperty.RegisterAttached<VirtualizingGrid, Control, object?>("RecycleKey");

	private static readonly object s_itemIsItsOwnContainer = new ();

	private readonly Dictionary<object, Stack<Control>>? _recyclePool = new ();

	private readonly Grid internalGrid = new ();

	/// <inheritdoc />
	public VirtualizingGrid()
	{
		Children.Add(internalGrid);
	}

	public ColumnDefinitions ColumnDefinitions
	{
		get => internalGrid.ColumnDefinitions;
		set => internalGrid.ColumnDefinitions = value;
	}

	public RowDefinitions RowDefinitions
	{
		get => internalGrid.RowDefinitions;
		set => internalGrid.RowDefinitions = value;
	}

	/// <inheritdoc />
	protected override Size MeasureOverride(Size availableSize)
	{
		RealizeElements(availableSize);
		return CalculateDesiredSize(availableSize);
	}

	private Size CalculateDesiredSize(Size availableSize)
	{
		internalGrid.Measure(availableSize);
		return internalGrid.DesiredSize;
	}

	private void RealizeElements(Size availableSize)
	{
		for (var index = 0; index < Items.Count; index++)
		{
			object? item = Items[index];
			Control control = RealizeElement(item, index);
			control.Measure(availableSize);
		}
	}

	private Control RealizeElement(object? item, int index)
	{
		Debug.Assert(ItemContainerGenerator != null, nameof(ItemContainerGenerator) + " != null");

		if (ItemContainerGenerator.NeedsContainer(item, index, out object? recycleKey))
		{
			return GetRecycledElement(item, index, recycleKey) ??
			       CreateElement(item, index, recycleKey);
		}
		else
		{
			return GetItemAsOwnContainer(item, index);
		}
	}

	private Control? GetRecycledElement(object? item, int index, object? recycleKey)
	{
		Debug.Assert(ItemContainerGenerator is not null);

		if (recycleKey is null)
			return null;

		ItemContainerGenerator generator = ItemContainerGenerator!;

		if (_recyclePool?.TryGetValue(recycleKey, out var recyclePool) == true && recyclePool.Count > 0)
		{
			Control recycled = recyclePool.Pop();
			recycled.IsVisible = true;
			generator.PrepareItemContainer(recycled, item, index);
			generator.ItemContainerPrepared(recycled, item, index);
			return recycled;
		}

		return null;
	}

	private Control CreateElement(object? item, int index, object? recycleKey)
	{
		Debug.Assert(ItemContainerGenerator is not null);

		ItemContainerGenerator generator = ItemContainerGenerator!;
		Control container = generator.CreateContainer(item, index, recycleKey);

		container.SetValue(RecycleKeyProperty, recycleKey);
		generator.PrepareItemContainer(container, item, index);
		AddItemAsChild(container);
		generator.ItemContainerPrepared(container, item, index);

		return container;
	}

	private Control GetItemAsOwnContainer(object? item, int index)
	{
		Debug.Assert(ItemContainerGenerator is not null);

		var controlItem = (Control)item!;
		ItemContainerGenerator generator = ItemContainerGenerator!;

		if (!controlItem.IsSet(RecycleKeyProperty))
		{
			generator.PrepareItemContainer(controlItem, controlItem, index);
			AddItemAsChild(controlItem);
			controlItem.SetValue(RecycleKeyProperty, s_itemIsItsOwnContainer);
			generator.ItemContainerPrepared(controlItem, item, index);
		}

		controlItem.IsVisible = true;
		return controlItem;
	}

	private void AddItemAsChild(Control control)
	{
		internalGrid.Children.Add(control);
	}

	/// <inheritdoc />
	protected override Control? ScrollIntoView(int index)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc />
	protected override Control? ContainerFromIndex(int index)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc />
	protected override int IndexFromContainer(Control container)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc />
	protected override IEnumerable<Control>? GetRealizedContainers()
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc />
	protected override IInputElement? GetControl(NavigationDirection direction, IInputElement? from, bool wrap)
	{
		throw new NotImplementedException();
	}
}