using Avalonia.Controls;

namespace Palladium.ActionsService;

public class TabsService
{
	private readonly Dictionary<Guid, TabItem> registeredActions = new();
	
	public TabControl? Target;

	public void HandleStartAction(ActionDescription action)
	{
		if (Target == null) throw new InvalidOperationException("Target has not been set.");

		if (!action.CanOpenMultiple && registeredActions.TryGetValue(action.Guid, out TabItem? registeredAction))
		{
			// change selected tab
			Target.SelectedItem = registeredAction;
		}
		else
		{
			// add new tab
			var contentControl = new ContentControl();
			var newTab = new TabItem
			{
				Content = contentControl,
				Header = $"{action.Emoji} {action.Title}"
			};
			Target.Items.Add(newTab);
			Target.SelectedItem = newTab;
			if (!action.CanOpenMultiple)
			{
				registeredActions.Add(action.Guid, newTab);
			}
			
			// invoke action
			action.OnStart?.Invoke(contentControl);
		}
	}
}