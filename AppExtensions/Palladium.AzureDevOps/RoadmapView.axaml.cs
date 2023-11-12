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
						
						Resources.TryGetValue(typeof(IterationViewModel), out object? iterationDataTemplateResource);
						if (iterationDataTemplateResource is DataTemplate iterationDataTemplate)
						{
							var controls = roadmapGridViewModel.IterationViewModels
								.Select(iterationViewModel =>
								{
									Control? control = iterationDataTemplate.Build(null);
									if (control is not null) control.DataContext = iterationViewModel;
									return control;
								})
								.Where(x => x != null);
							Grid.Children.AddRange(controls!);
						}
					}
				})
				.DisposeWith(disposables);
		});
	}

	/// <inheritdoc />
	public void Dispose()
	{
		DataContext = null;
	}
}