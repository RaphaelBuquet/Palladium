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