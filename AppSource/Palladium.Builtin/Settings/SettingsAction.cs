using Avalonia.Controls;
using Palladium.ActionsService;
using Palladium.Settings;

namespace Palladium.Builtin.Settings;

public class SettingsAction
{
	public static readonly Guid Guid = new ("2D91D690-3C94-4C82-A165-2CFA940977E7");
	private SettingsViewModel? settingsViewModel;

	public ActionDescription Description => new(Guid)
	{
		Guid = Guid,
		Title = "Settings",
		Description = "Settings.",
		Emoji = "⚙️",
		CanOpenMultiple = false,
		OnStart = Start
	};

	public void Init(SettingsService settingsService)
	{
		settingsViewModel = new SettingsViewModel(settingsService);
	}

	private void Start(ContentControl container)
	{
		var view = new SettingsView
		{
			DataContext = settingsViewModel
		};
		container.Content = view;
	}
}