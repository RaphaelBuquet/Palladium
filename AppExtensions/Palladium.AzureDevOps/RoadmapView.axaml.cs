using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Templates;
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
					foreach (GridLength gridLength in roadmapGridViewModel.Rows)
					{
						rowDefinitions.Add(new RowDefinition(gridLength));
					}
					foreach (GridLength gridLength in roadmapGridViewModel.Columns)
					{
						columnDefinitions.Add(new ColumnDefinition(gridLength));
					}
					if (Grid is not null)
					{
						Grid.RowDefinitions = rowDefinitions;
						Grid.ColumnDefinitions = columnDefinitions;

						Grid.Children.Clear();

						PopulateIterations(roadmapGridViewModel);
						PopulateWorkItems(roadmapGridViewModel);
					}
				})
				.DisposeWith(disposables);

			Disposable.Create(() =>
			{
				if (Grid is not null) Grid.Children.Clear();
			}).DisposeWith(disposables);
		});
	}

	private void PopulateIterations(RoadmapGridViewModel roadmapGridViewModel)
	{
		Resources.TryGetValue(typeof(IterationViewModel), out object? iterationDataTemplateResource);
		if (iterationDataTemplateResource is DataTemplate dataTemplate)
		{
			var iterationControls = roadmapGridViewModel.IterationViewModels
				.Select(vm =>
				{
					Control? control = dataTemplate.Build(null);
					if (control is not null) control.DataContext = vm;
					return control;
				})
				.Where(x => x != null);
			Grid.Children.AddRange(iterationControls!);
		}
	}

	private void PopulateWorkItems(RoadmapGridViewModel roadmapGridViewModel)
	{
		Resources.TryGetValue(typeof(WorkItemViewModel), out object? workItemDataTemplateResource);
		if (workItemDataTemplateResource is DataTemplate dataTemplate)
		{
			var workItemControls = roadmapGridViewModel.WorkItemViewModels
				.Select(vm =>
				{
					Control? control = dataTemplate.Build(null);
					if (control is not null) control.DataContext = vm;
					return control;
				})
				.Where(x => x != null);
			Grid.Children.AddRange(workItemControls!);
		}
	}

	/// <inheritdoc />
	public void Dispose()
	{
		DataContext = null;
	}
}