using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using DynamicData;
using Palladium.ActionsService;
using Palladium.Settings;

namespace Palladium.BuiltinActions.SearchOverride;

public class SearchOverrideAction
{
	public static readonly Guid Guid = new ("067fb8e2-fd37-49bc-b15b-6392fe75b550");

	public ActionDescription Description => new(Guid)
	{
		Guid = Guid,
		Title = "Search Override",
		Description = "Override \u229e Win + S to search the web in your default web browser.",
		Emoji = "🔎",
		CanOpenMultiple = false,
		OnStart = Start
	};

	public void Init(ActionsRepositoryService repositoryService, SettingsService settingsService)
	{
		repositoryService.Actions.AddOrUpdate(Description);

		var settingsVM = new SearchOverrideSettingsViewModel(settingsService);
		var settingsView = new SearchOverrideSettingsView { DataContext = settingsVM };
		_ = settingsService.Install(settingsVM, settingsView, true);
	}

	private void Start(ContentControl container)
	{
		if (!(Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop))
		{
			container.Content = new TextBlock
			{
				Text = "Not supported."
			};
			return;
		}

		var view = new SearchOverrideView
		{
			DataContext = new SearchOverrideViewModel()
		};
		container.Content = view;

		// ensure windows API clean up
		desktop.Exit += (sender, args) => view.DataContext = null;
	}
}