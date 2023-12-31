﻿using System;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Alias;
using DynamicData.Binding;
using Palladium.ActionsService;
using Palladium.ActionsService.ViewModels;
using Palladium.ExtensionFunctions.Lifecycle;
using ReactiveUI;

namespace Palladium.ViewModels;

public class HomeViewModel : ReactiveObject, IActivatableViewModel, ILifecycleAwareViewModel
{
	/// <summary>
	///     Design preview constructor.
	/// </summary>
	public HomeViewModel() : this(null, null)
	{ }

	public HomeViewModel(ActionsRepositoryService? actionsRepositoryService, TabsService? tabsService)
	{
		Actions = new ObservableCollectionExtended<ActionViewModel>();
		IDisposable? connection = null;
		if (actionsRepositoryService != null && tabsService != null)
		{
			connection = actionsRepositoryService.Actions
				.Connect()
				.Where(x => x.Guid != new Guid("00000000-FFFF-EEEE-DDDD-000000000000")) // prevent adding extension actions that are using the default GUID because someone forgot to update it.
				.Transform(description => new ActionViewModel(description, tabsService))
				.ObserveOn(RxApp.MainThreadScheduler) // this seems to solve actions being created multiple times by the ItemsControl
				.Bind(Actions)
				.Subscribe();
		}

		this.WhenAttached(disposables =>
		{
			if (connection is not null) disposables.Add(connection);
		});
	}

	public ObservableCollectionExtended<ActionViewModel> Actions { get; }

	/// <inheritdoc />
	ViewModelActivator IActivatableViewModel.Activator { get; } = new ();

	/// <inheritdoc />
	public LifecycleActivator Activator { get; } = new();
}