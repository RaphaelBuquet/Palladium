using Avalonia.Controls;
using Palladium.ActionsService;
using Palladium.Logging;

namespace Palladium.Builtin.ImmersiveGame;

public class ImmersiveGameAction
{
	private readonly Log? log;

	public ImmersiveGameAction(Log? log)
	{
		this.log = log;
	}

	public ActionDescription Description => new (new Guid("b11e7aca-1d85-4b7e-acce-ab99efaf5029"))
	{
		Title = "Immersive Game",
		Description = "Turn off all monitors except the main monitor for an immersive gaming experience.",
		Emoji = "🎮",
		CanOpenMultiple = false,
		OnStart = Start
	};

	public void Start(ContentControl container)
	{
		var immersiveGameViewModel = new ImmersiveGameViewModel(new WindowsDisplays(log));
		container.Content = new ImmersiveGameControl
		{
			DataContext = immersiveGameViewModel
		};
		immersiveGameViewModel.RefreshAvailableDisplays();
	}
}