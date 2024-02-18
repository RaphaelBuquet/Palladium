using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using ReactiveUI;

namespace Palladium.Controls;

/// <summary>
///     Improve performance over Grid by estimating column and row sizes to exclude
/// </summary>
public class VirtualizingGrid : Grid, ILogicalScrollable
{
	public static readonly StyledProperty<double> ItemHeightEstimateProperty = AvaloniaProperty.Register<VirtualizingGrid, double>(
		nameof(ItemHeightEstimate), 100);

	public static readonly StyledProperty<double> ItemWidthEstimateProperty = AvaloniaProperty.Register<VirtualizingGrid, double>(
		nameof(ItemWidthEstimate));

	public static readonly StyledProperty<IReadOnlyList<object>> ItemsSourceProperty =
		AvaloniaProperty.Register<ItemsControl, IReadOnlyList<object>>(nameof(ItemsSource), new List<object>());

	private Vector offset = Vector.Zero;
	private readonly ColumnAndRowSizeEstimates columnAndRowSizeEstimates = new ();

	/// <inheritdoc />
	public event EventHandler? ScrollInvalidated;

	/// <inheritdoc />
	public VirtualizingGrid()
	{
		this.WhenAnyValue(x => x.ItemsSource)
			.Subscribe(_ =>
			{
				Children.Clear();
				for (var index = 0; index < ItemsSource.Count; index++)
				{
					object item = ItemsSource[index];
					Control control = CreateElement(item);
					AddItemAsChild(control);
				}
			});
		this.WhenAnyValue(x => x.ItemHeightEstimate, x => x.ItemWidthEstimate)
			.Subscribe(_ => { InvalidateMeasure(); });
	}

	public IReadOnlyList<object> ItemsSource
	{
		get => GetValue(ItemsSourceProperty);
		set => SetValue(ItemsSourceProperty, value);
	}

	public double ItemHeightEstimate
	{
		get => GetValue(ItemHeightEstimateProperty);
		set => SetValue(ItemHeightEstimateProperty, value);
	}

	public double ItemWidthEstimate
	{
		get => GetValue(ItemWidthEstimateProperty);
		set => SetValue(ItemWidthEstimateProperty, value);
	}

	/// <inheritdoc />
	public Size Extent { get; private set; }


	/// <inheritdoc />
	public Vector Offset
	{
		get => offset;
		set
		{
			if (value == offset) return;
			offset = value;
			RealizeElements();
		}
	}

	/// <inheritdoc />
	public Size Viewport => Bounds.Size;

	/// <inheritdoc />
	public bool CanHorizontallyScroll { get; set; } = true;

	/// <inheritdoc />
	public bool CanVerticallyScroll { get; set; } = true;

	/// <inheritdoc />
	public bool IsLogicalScrollEnabled => true;

	/// <inheritdoc />
	public Size ScrollSize => new (ItemWidthEstimate, ItemHeightEstimate);

	/// <inheritdoc />
	public Size PageScrollSize => Viewport - ScrollSize;

	/// <inheritdoc />
	protected override Size MeasureOverride(Size availableSize)
	{
		// Note: this function will be called when columns or rows are changed.
		CalculateExtent(availableSize);
		RealizeElements();
		base.MeasureOverride(Extent);
		return Extent;
	}

	private void CalculateExtent(Size availableSize)
	{
		columnAndRowSizeEstimates.ColumnSizes.Clear();
		columnAndRowSizeEstimates.RowSizes.Clear();

		var columnEstimates = columnAndRowSizeEstimates.ColumnSizes;
		var columnGridLengths = ColumnDefinitions.Select(c => c.Width);
		double widthEstimate = CalculateGridWidthEstimate(availableSize, columnGridLengths, columnEstimates, ItemWidthEstimate);
		var rowEstimates = columnAndRowSizeEstimates.RowSizes;
		var rowGridLengths = RowDefinitions.Select(row => row.Height);
		double heightEstimate = CalculateGridWidthEstimate(availableSize, rowGridLengths, rowEstimates, ItemHeightEstimate);

		Extent = new Size(widthEstimate, heightEstimate);
	}

	private static double CalculateGridWidthEstimate(  Size availableSize, IEnumerable<GridLength> gridLengths, List<double> estimates, double itemWidthEstimate)
	{
		double widthEstimate = 0;
		double starCount = 0;
		foreach (GridLength gridLength in gridLengths)
		{
			switch (gridLength)
			{
				case { IsStar: true }:
				{
					starCount += gridLength.Value;
					break;
				}
				case { IsAuto: true }:
				{
					widthEstimate += itemWidthEstimate;
					break;
				}
				case { IsAbsolute: true }:
				{
					widthEstimate += gridLength.Value;
					break;
				}
			}
		}
		double availableWidthForStarColumns = availableSize.Width - widthEstimate;
		if (starCount > 0)
		{
			widthEstimate = availableSize.Width;
		}
		foreach (GridLength gridLength in gridLengths)
		{
			switch (gridLength)
			{
				case { IsStar: true }:
				{
					double proportion = starCount > 0 ? gridLength.Value / starCount : 0;
					double width = proportion * availableWidthForStarColumns;
					width = Math.Max(0, width);
					estimates.Add(width);

					break;
				}
				case { IsAuto: true }:
				{
					estimates.Add(itemWidthEstimate);
					break;
				}
				case { IsAbsolute: true }:
				{
					estimates.Add(gridLength.Value);
					break;
				}
			}
		}
		return widthEstimate;
	}

	private void RealizeElements()
	{
		// TODO
		// in my case the columns have a known pixel width but rows have an auto height.
		// for each column/row use the known width/height or guess, say 100.
		// then somehow get the "first" visible column, and for each visible row of that column, populate items.
		// the "somehow" needs to be done with the logical scrollable interface surely. find examples of that maybe.
		// create interface elements have to implement to expose grid column and row. in cases where the span is more than 1 then consider that item to be in multiple cells.
		// need a sort of spacial structure to get these items.

		// TODO: recycling with a ItemsControl-like approach would probably be better.

		VisibilityEstimates visibilityEstimates = CalculateFirstAndLastVisibleIndexes();

		foreach (Control? child in Children)
		{
			int rowIndex = child.GetValue(RowProperty);
			int columnIndex = child.GetValue(ColumnProperty);
			if (columnIndex < visibilityEstimates.FirstVisibleColumnIndex
			    || rowIndex < visibilityEstimates.FirstVisibleRowIndex
			    || columnIndex > visibilityEstimates.LastVisibleColumnIndex
			    || rowIndex > visibilityEstimates.LastVisibleRowIndex)
			{
				child.IsVisible = false;
			}
			else
			{
				child.IsVisible = true;
			}
		}
	}

	private VisibilityEstimates CalculateFirstAndLastVisibleIndexes()
	{
		var visibilityEstimate = new VisibilityEstimates(ColumnDefinitions.Count, RowDefinitions.Count);

		CalculateVisibility(visibilityEstimate,
			columnAndRowSizeEstimates.ColumnSizes,
			Offset.X,
			Offset.X + Viewport.Width,
			out visibilityEstimate.FirstVisibleColumnIndex,
			out visibilityEstimate.LastVisibleColumnIndex);

		CalculateVisibility(visibilityEstimate,
			columnAndRowSizeEstimates.RowSizes,
			Offset.Y,
			Offset.Y + Viewport.Height,
			out visibilityEstimate.FirstVisibleRowIndex,
			out visibilityEstimate.LastVisibleRowIndex);

		return visibilityEstimate;
	}

	private static void CalculateVisibility(
		VisibilityEstimates visibilityEstimate,
		List<double> sizes,
		double viewportStart,
		double viewportEnd,
		out int firstVisibleIndex,
		out int lastVisibleIndex)
	{
		double accumulatedWidth = 0;
		firstVisibleIndex = visibilityEstimate.FirstVisibleColumnIndex;
		lastVisibleIndex = visibilityEstimate.LastVisibleColumnIndex;
		for (var index = 0; index < sizes.Count; index++)
		{
			double width = sizes[index];
			accumulatedWidth += width;
			if (accumulatedWidth < viewportStart)
			{
				firstVisibleIndex = index;
			}
			if (accumulatedWidth >= viewportEnd)
			{
				lastVisibleIndex = index;
				break;
			}
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

	/// <inheritdoc />
	public bool BringIntoView(Control target, Rect targetRect)
	{
		// This method is used to scroll a control into the view. Based on the control passed in as the target, you'd adjust the Offset to make sure the control and the specified Rect are visible.
		throw new NotImplementedException();
	}

	/// <inheritdoc />
	public Control? GetControlInDirection(NavigationDirection direction, Control? from)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc />
	public void RaiseScrollInvalidated(EventArgs e)
	{
		ScrollInvalidated?.Invoke(this, e);
	}

	private struct ColumnAndRowSizeEstimates
	{
		public List<double> ColumnSizes = new ();
		public List<double> RowSizes = new ();

		public ColumnAndRowSizeEstimates()
		{ }
	}

	private struct VisibilityEstimates(int columnCount, int rowCount)
	{
		public int FirstVisibleColumnIndex = 0;
		public int FirstVisibleRowIndex = 0;
		public int LastVisibleColumnIndex = columnCount - 1;
		public int LastVisibleRowIndex = rowCount - 1;
	}
}