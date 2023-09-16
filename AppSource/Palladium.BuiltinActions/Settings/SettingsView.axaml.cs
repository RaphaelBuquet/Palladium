using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;

namespace Palladium.BuiltinActions.Settings;

public partial class SettingsView : ReactiveUserControl<SettingsViewModel>
{
	public SettingsView()
	{
		InitializeComponent();
	}
}