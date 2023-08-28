using Avalonia.Controls;
using Palladium.ActionsService;

namespace Palladium.BuiltinActions.ImmersiveGame;

public class ImmersiveGameAction
{
	public ActionDescription Description => new()
	{
		Title = "Immersive Game",
		Description = "Turn off all monitors except the main monitor for an immersive gaming experience.",
		Emoji = "🎮",
		CanOpenMultiple = false,
		OnStart = Start
	};

	public void Start(ContentControl container)
	{
		var immersiveGameViewModel = new ImmersiveGameViewModel(new WindowsDisplays());
		container.Content = new ImmersiveGameControl
		{
			DataContext = immersiveGameViewModel
		};
		immersiveGameViewModel.RefreshAvailableDisplays();
	}
}