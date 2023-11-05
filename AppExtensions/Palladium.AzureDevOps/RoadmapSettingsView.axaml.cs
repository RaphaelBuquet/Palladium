using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace Palladium.AzureDevOps;

public partial class RoadmapSettingsView : ReactiveUserControl<RoadmapSettingsViewModel>
{
	public RoadmapSettingsView()
	{
		InitializeComponent();
		this.WhenActivated(disposables =>
		{
			this.Bind(ViewModel,
					viewModel => viewModel.OrganisationUrl,
					view => view.OrganisationUrl.Text)
				.DisposeWith(disposables);
		});
	}
}