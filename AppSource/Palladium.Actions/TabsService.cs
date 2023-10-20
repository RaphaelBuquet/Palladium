using System.Collections.ObjectModel;
using System.Reactive.Subjects;
using System.Windows.Input;
using Avalonia.Controls;
using Palladium.Controls;
using ReactiveUI;

namespace Palladium.ActionsService;

public class TabsService
{
	private readonly Dictionary<Guid, ApplicationTabItem> registeredActions = new();
	private readonly ReplaySubject<ApplicationTabItem?> currentTab = new (1);

	public ObservableCollection<ApplicationTabItem> Tabs { get; } = new ();

	public IObservable<ApplicationTabItem?> CurrentTab => currentTab;

	public void HandleStartAction(ActionDescription action)
	{
		if (!action.CanOpenMultiple && registeredActions.TryGetValue(action.Guid, out ApplicationTabItem? registeredAction))
		{
			// change selected tab
			currentTab.OnNext(registeredAction);
		}
		else
		{
			// add new tab
			var contentControl = new ContentControl();
			var newTab = new ApplicationTabItem
			{
				Content = contentControl,
				Header = $"{action.Emoji} {action.Title}"
			};
			newTab.CloseTabCommand = CloseTab(contentControl, newTab, action.Guid);
			Tabs.Add(newTab);
			currentTab.OnNext(newTab);
			if (!action.CanOpenMultiple)
			{
				registeredActions.Add(action.Guid, newTab);
			}

			// invoke action
			action.OnStart?.Invoke(contentControl);
		}
	}

	private ICommand CloseTab(ContentControl contentControl, ApplicationTabItem tab, Guid actionGuid)
	{
		return ReactiveCommand.Create(() =>
		{
			if (contentControl.Content is IDisposable disposable)
			{
				disposable.Dispose();
			}
			Tabs.Remove(tab);
			registeredActions.Remove(actionGuid);
		});
	}
}