using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Palladium.Settings;

namespace Palladium.BuiltinActions.Settings;

public class SettingsViewModel
{
	public SettingsViewModel(SettingsService settingsService)
	{
		throw new NotImplementedException();
	}

	public SettingsViewModel()
	{
		if (Design.IsDesignMode)
		{
			Settings.Add(new SettingsEntryViewModel("Palladium Application", "⚙️ Palladium Application", new StackPanel
			{
				Spacing = 6,
				Orientation = Orientation.Horizontal,
				Children =
				{
					new ToggleButton(),
					new TextBlock { Text = "Launch at startup" }
				}
			})
			{
				TitleFontWeight = FontWeight.Bold
			});
			Settings.Add(new SettingsEntryViewModel("Search Override", "🔎 Search Override", "Not implemented."));
		}
	}


	public ObservableCollection<SettingsEntryViewModel> Settings { get; } = new ();
}