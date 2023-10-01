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
			this.BindValidation(
					ViewModel,
					vm => vm.LaunchAtStartup,
					LaunchAtStartup)
				.DisposeWith(disposables);
		});
	}
}