using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using Palladium.ExtensionFunctions.Lifecycle;
using ReactiveUI;

namespace Palladium.AzureDevOps;

public partial class RoadmapView : ReactiveUserControl<RoadmapViewModel>, IDisposable
{
	public RoadmapView()
	{
		InitializeComponent();
		this.InstallLifecycleHandler();

		this.WhenActivated(disposables =>
		{
			this.WhenAnyValue(x => x.ViewModel!.RoadmapGridViewModel)
				.ObserveOn(RxApp.MainThreadScheduler)
				.Subscribe(roadmapGridViewModel =>
				{
					var rowDefinitions = new RowDefinitions();
					var columnDefinitions = new ColumnDefinitions();
					rowDefinitions.AddRange(roadmapGridViewModel.Rows.Select(gridLength => new RowDefinition(gridLength)));
					columnDefinitions.AddRange(roadmapGridViewModel.Columns.Select(gridLength => new ColumnDefinition(gridLength)));
					Grid grid = GetGrid();
					grid.RowDefinitions = rowDefinitions;
					grid.ColumnDefinitions = columnDefinitions;

					var viewModels = new List<object>();
					viewModels.AddRange(roadmapGridViewModel.IterationViewModels);
					viewModels.AddRange(roadmapGridViewModel.WorkItemViewModels);
					ItemsControl.ItemsSource = viewModels;
				})
				.DisposeWith(disposables);

			this.WhenAnyValue(x => x.ViewModel!.RoadmapGridViewModel)
				.Skip(1) // without this, this will override the DefaultScrollbarNormalisedPosition offset code
				.Select(CalculateGridWidth)
				.CombineLatest(
					this.WhenAnyValue(x => x.ViewModel!.ZoomLevel),
					(sizeSum, zoom) => (sizeSum, zoom))
				.Select(pair => pair.sizeSum * 10 * pair.zoom)
				.ObserveOn(RxApp.MainThreadScheduler)
				.Subscribe(newWidth =>
				{
					Grid grid = GetGrid();
					double widthBefore = grid.Width;
					grid.Width = newWidth;
					if (widthBefore > 0)
					{
						double extentBefore = ScrollViewer.Extent.Width;
						double diff = extentBefore - widthBefore;
						double extentAfter = newWidth + diff;
						double newOffset = CalculateScrollViewerOffsetOnZoomChange(extentBefore, extentAfter, ScrollViewer.Viewport.Width, ScrollViewer.Offset.X);
						ScrollViewer.Offset = new Vector(newOffset, ScrollViewer.Offset.Y);
					}
				})
				.DisposeWith(disposables);

			// TODO: there is a visual artefact with this, where for a frame the grid is shown with the scrollbar at the beginning.
			this.WhenAnyValue(x => x.ViewModel!.DefaultScrollbarNormalisedPosition)
				.ObserveOn(RxApp.MainThreadScheduler)
				.Subscribe(defaultScrollbarNormalisedPosition =>
				{
					Size extent = ScrollViewer.Extent;
					double offsetX = defaultScrollbarNormalisedPosition.X * extent.Width;
					double offsetY = defaultScrollbarNormalisedPosition.Y * extent.Height;

					// TODO center content
					// For the X offset we want the normalised position to represent the middle of the screen,
					// such that for a value of 0.5 the viewport will be centered,
					// rather than being offset a bit to the right.
					ScrollViewer.Offset = new Vector(offsetX, offsetY);
				})
				.DisposeWith(disposables);

			Disposable.Create(() =>
			{
				// empty the grid when it goes out of view.
				// that's because it is automatically refreshed when it goes into view, it would look weird if 
				// it didn't get emptied.
				ItemsControl.ItemsSource = new List<object>();
			}).DisposeWith(disposables);
		});
	}

	private static double CalculateGridWidth(RoadmapGridViewModel roadmapGridViewModel)
	{
		double total = 0;
		foreach (GridLength column in roadmapGridViewModel.Columns)
		{
			Debug.Assert(column.IsStar);
			total += column.Value;
		}
		return total;
	}

	private static double CalculateScrollViewerOffsetOnZoomChange(double extentBefore, double extentAfter, double viewportSize, double existingOffset)
	{
		// clamp viewport size, in case it is bigger than the extent.
		// viewportSize = Math.Min(viewportSize, extentBefore);

		double centeredOffsetBefore = existingOffset + viewportSize / 2.0;
		double normalisedOffsetBefore = InvertLerp(0, extentBefore, centeredOffsetBefore);
		double centeredOffsetAfter = Lerp(0, extentAfter, normalisedOffsetBefore);
		double offsetAfter = centeredOffsetAfter - viewportSize / 2.0;
		return offsetAfter;
	}

	private static double Lerp(double a, double b, double x)
	{
		return a + x * (b - a);
	}

	private static double InvertLerp(double a, double b, double x)
	{
		return (x - a) / (b - a);
	}

	/// <inheritdoc />
	public void Dispose()
	{
		DataContext = null;
	}

	private Grid GetGrid()
	{
		Debug.Assert(ItemsControl.ItemsPanelRoot != null, "ItemsControl.ItemsPanelRoot != null");
		return (Grid)ItemsControl.ItemsPanelRoot;
	}
}