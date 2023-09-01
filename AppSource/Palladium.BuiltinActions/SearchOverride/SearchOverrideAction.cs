using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Palladium.ActionsService;

namespace Palladium.BuiltinActions.SearchOverride;

public class SearchOverrideAction
{
	public ActionDescription Description => new()
	{
		Title = "Search Override",
		Description = "Override \u229e Win + S to search the web in your default web browser.",
		Emoji = "🔎",
		CanOpenMultiple = false,
		OnStart = Start
	};

	private void Start(ContentControl container)
	{
		var vm = new SearchOverrideViewModel();
		if (!(Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop))
		{
			container.Content = new TextBlock
			{
				Text = "Not supported."
			};
			return;
		}

		container.Content = new SearchOverrideView
		{
			DataContext = vm
		};

		// ensure windows API clean up 
		desktop.Exit += (sender, args) => WindowsKeyboard.UnsetHook();
	}
}