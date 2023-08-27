using System;
using DynamicData;
using DynamicData.Binding;
using Palladium.ActionsService;
using Palladium.ActionsService.ViewModels;
using Palladium.Tabs;
using ReactiveUI;

namespace Palladium.ViewModels;

public class HomeViewModel : ReactiveObject, IActivatableViewModel
{
	/// <summary>
	/// Design preview constructor.
	/// </summary>
	public HomeViewModel() : this(null, null)
	{ }

	public HomeViewModel(ActionsRepositoryService? actionsRepositoryService, TabsService? tabsService)
	{
		Actions = new();
		IDisposable? connection = null;
		if (actionsRepositoryService != null && tabsService != null)
		{
			connection = actionsRepositoryService.Actions
				.Connect()
				.Transform(description => new ActionViewModel(description, tabsService))
				.Bind(Actions)
				.Subscribe();
		}

		this.WhenActivated(disposables =>
		{
			if (connection is not null) disposables.Add(connection);
		});
	}

	public ObservableCollectionExtended<ActionViewModel> Actions { get; }

	/// <inheritdoc />
	ViewModelActivator IActivatableViewModel.Activator { get; } = new ();
}