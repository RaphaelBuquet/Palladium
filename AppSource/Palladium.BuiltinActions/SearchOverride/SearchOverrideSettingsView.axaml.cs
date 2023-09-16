using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;

namespace Palladium.BuiltinActions.SearchOverride;

public partial class SearchOverrideSettingsView : ReactiveUserControl<SearchOverrideSettingsViewModel>
{
	public SearchOverrideSettingsView()
	{
		InitializeComponent();
	}
}