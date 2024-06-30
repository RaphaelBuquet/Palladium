using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using Palladium.ExtensionFunctions;
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

			// handle change of VM: set the scroll offset and set the grid width
			this.WhenAnyValue(x => x.ViewModel!.RoadmapGridViewModel)
				.CombineLatestNoEmit(
					this.WhenAnyValue(x => x.ViewModel!.ZoomLevel),
					(vm, zoom) => (vm, zoom))
				.ObserveOn(RxApp.MainThreadScheduler)
				.Subscribe(pair =>
				{
					Grid grid = GetGrid();

					// calculate this here manually, as Avalonia may not have done the layout pass yet
					double newGridWidth = CalculateGridWidth(pair.vm, pair.zoom);
					grid.Width = newGridWidth;
					var newScrollViewerExtent = new Size(
						newGridWidth + ScrollViewerMargin.Left + ScrollViewerMargin.Right,
						ScrollViewer.Extent.Height);

					Size viewport = ScrollViewer.Viewport;
					if (pair.vm.InitialScrollbarNormalisedPosition.HasValue)
					{
						Vector scrollbarPos = pair.vm.InitialScrollbarNormalisedPosition.Value;
						// do some processing on the X offset to make sure the content can be centered on the screen 
						// for a value of 0.5
						double offsetX = scrollbarPos.X * newScrollViewerExtent.Width - viewport.Width / 2.0;
						double offsetY = scrollbarPos.Y * newScrollViewerExtent.Height;

						// Delay to next frame
						// TODO with a custom transition this would probably not be needed (avalonia currently clamps the offset before the extent has been changed by the grid's width being set)
						// TODO the transition should be turned off when setting this specific value, because we don't want a transition when setting the initial value. this currently creates a flicker.
						Vector newOffsetToApplyNextFrame = ClampOffset(new Vector(offsetX, offsetY), newScrollViewerExtent, viewport);
						Dispatcher.UIThread.InvokeAsync(() => { ScrollViewer.Offset = newOffsetToApplyNextFrame; });
					}
				})
				.DisposeWith(disposables);

			// Handle zoom: set the grid's width,
			// and update the scroll offset as well so that the zoom is centered in the middle of the screen.
			// This is separate from the one above as it needs to read InitialScrollbarNormalisedPosition when it 
			// has been set for the first time. Using CombineLatest would repeat it and it would reset the initial
			// scrollbar position when the user tries to zoom/unzoom...
			this.WhenAnyValue(x => x.ViewModel!.ZoomLevel)
				.CombineLatestNoEmit(
					this.WhenAnyValue(x => x.ViewModel!.RoadmapGridViewModel),
					(zoom, vm) => (zoom, vm))
				.ObserveOn(RxApp.MainThreadScheduler)
				.Subscribe(pair =>
				{
					Grid grid = GetGrid();

					double newGridWidth = CalculateGridWidth(pair.vm, pair.zoom);
					grid.Width = newGridWidth;

					// calculate this here manually, as Avalonia has not done the layout pass yet
					var newScrollViewerExtent = new Size(
						newGridWidth + ScrollViewerMargin.Left + ScrollViewerMargin.Right,
						ScrollViewer.Extent.Height);

					Size viewport = ScrollViewer.Viewport;

					double extentBefore = ScrollViewer.Extent.Width;
					double extentAfter = newGridWidth + ScrollViewerMargin.Left + ScrollViewerMargin.Right;
					double existingOffset = ScrollViewer.Offset.X;
					double newOffsetX = CalculateScrollViewerOffsetOnZoomChange(extentBefore, extentAfter, viewport.Width, existingOffset);

					// TODO: this when the offset is 0 or all the way to the max. When clicking unzoom or zoom respectively, it will cause a flicker.
					// the solution is probably a custom transition
					ScrollViewer.Offset = ClampOffset(new Vector(newOffsetX, ScrollViewer.Offset.Y), newScrollViewerExtent, viewport);
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

	/// <summary>
	///     This is needed in C# to calculate the roadmap grid width and scroll viewer width correctly, before avalonia has run
	///     the layout pass for them.
	/// </summary>
	public static Thickness ScrollViewerMargin => new (14, 0, 14, 40);

	private static Vector ClampOffset(Vector offset, Size extent, Size viewport)
	{
		return new Vector(
			Math.Clamp(offset.X, 0, Math.Max(0, extent.Width - viewport.Width)),
			Math.Clamp(offset.Y, 0, Math.Max(0, extent.Height - viewport.Height))
		);
	}

	private static double CalculateGridWidth(RoadmapGridViewModel roadmapGridViewModel, double zoom)
	{
		double total = 0;
		foreach (GridLength column in roadmapGridViewModel.Columns)
		{
			Debug.Assert(column.IsStar);
			total += column.Value;
		}
		return total * zoom * 10;
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