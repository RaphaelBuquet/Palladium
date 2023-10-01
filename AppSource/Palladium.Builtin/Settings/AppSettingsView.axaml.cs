using Avalonia.Controls;
using Avalonia.ReactiveUI;
using ReactiveUI;
using ReactiveUI.Validation.Extensions;
using Palladium.ObservableExtensions;

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
				view => view.LaunchAtStartupErrors);
		});
	}

	private string? LaunchAtStartupErrors
	{
		get => LaunchAtStartup.GetAttachedValidation();
		set => LaunchAtStartup.SetAttachedValidation(value);
	}
}