using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using Palladium.Controls;
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
					if (VirtualizingGrid is not null)
					{
						VirtualizingGrid.RowDefinitions = rowDefinitions;
						VirtualizingGrid.ColumnDefinitions = columnDefinitions;

						var viewModels = new List<object>();
						viewModels.AddRange(roadmapGridViewModel.IterationViewModels);
						viewModels.AddRange(roadmapGridViewModel.WorkItemViewModels);
						ItemsControl.ItemsSource = viewModels;
					}
				})
				.DisposeWith(disposables);

			Disposable.Create(() =>
			{
				if (ItemsControl is not null)
				{
					ItemsControl.ItemsSource = null;
				}
			}).DisposeWith(disposables);
		});
	}

	private VirtualizingGrid? VirtualizingGrid => ItemsControl?.ItemsPanelRoot as VirtualizingGrid;

	/// <inheritdoc />
	public void Dispose()
	{
		DataContext = null;
	}
}