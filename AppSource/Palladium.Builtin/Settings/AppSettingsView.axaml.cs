using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using Palladium.ExtensionFunctions;
using ReactiveUI;
using ReactiveUI.Validation.Extensions;

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
				view => view.LaunchAtStartupErrors)
				.DisposeWith(disposables);
		});
	}

	private string? LaunchAtStartupErrors
	{
		get => LaunchAtStartup.GetAttachedValidation();
		set => LaunchAtStartup.SetAttachedValidation(value);
	}
}