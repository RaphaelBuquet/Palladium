using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using Palladium.ExtensionFunctions;
using ReactiveUI;

namespace Palladium.Builtin.Settings;

public partial class AppSettingsView : ReactiveUserControl<AppSettingsViewModel>
{
	public AppSettingsView()
	{
		InitializeComponent();

		this.WhenActivated(disposables =>
		{
			// bind validation to checkboxes so they appear red when there is a validation error
			this.BindValidation(
					ViewModel,
					vm => vm.LaunchAtStartup,
					LaunchAtStartupCheckbox)
				.DisposeWith(disposables);
			this.BindValidation(
					ViewModel,
					vm => vm.LaunchAtStartup,
					StartMinimisedCheckbox)
				.DisposeWith(disposables);
		});
	}
}