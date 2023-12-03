using System.Reactive.Disposables;
using System.Reactive.Linq;
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
					VirtualizingGrid.RowDefinitions = rowDefinitions;
					VirtualizingGrid.ColumnDefinitions = columnDefinitions;

					var viewModels = new List<object>();
					viewModels.AddRange(roadmapGridViewModel.IterationViewModels);
					viewModels.AddRange(roadmapGridViewModel.WorkItemViewModels);
					VirtualizingGrid.ItemsSource = viewModels;
				})
				.DisposeWith(disposables);

			Disposable.Create(() =>
			{
				// empty the grid when it goes out of view.
				// that's because it is automatically refreshed when it goes into view, it would look weird if 
				// it didn't get emptied.
				VirtualizingGrid.ItemsSource = new List<object>();
			}).DisposeWith(disposables);
		});
	}

	/// <inheritdoc />
	public void Dispose()
	{
		DataContext = null;
	}
}