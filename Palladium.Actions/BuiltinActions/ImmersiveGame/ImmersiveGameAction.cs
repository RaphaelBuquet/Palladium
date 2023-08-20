using Avalonia.Controls;

namespace Palladium.ActionsService.BuiltinActions;

public class ImmersiveGameAction
{
	public ActionDescription Description => new()
	{
		Title = "Immersive Game",
		Description = "Turn off all monitors except the main monitor for an immersive gaming experience.",
		Emoji = "🎮",
		OnStart = Start
	};
	
	public void Start(ContentControl container)
	{
		container.Content = new ImmersiveGameControl();
	}
}