﻿using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Avalonia.Controls.Documents;
using Palladium.ActionsService;
using Palladium.Controls;
using Palladium.ExtensionFunctions;
using Palladium.ExtensionFunctions.Lifecycle;
using ReactiveUI;
using MiniLog = Palladium.Logging.MiniLog;

namespace Palladium.Builtin.ImmersiveGame;

public class ImmersiveGameViewModel : ReactiveObject, IActivatableViewModel, ILifecycleAwareViewModel
{
	private readonly IDisplaySource? source;
	private readonly BehaviorSubject<Task<string[]>> displays;

	private readonly ObservableAsPropertyHelper<string> availableDisplays;

	private readonly ObservableAsPropertyHelper<bool> isWorking;
	private readonly ReplayFirstValuesSubject<Inline> outputStream = new (2);

	/// <inheritdoc />
	public ImmersiveGameViewModel() : this(null)
	{ }

	public ImmersiveGameViewModel(IDisplaySource? source)
	{
		this.source = source;
		ActivateCommand = ReactiveCommand.Create(Activate);
		DeactivateCommand = ReactiveCommand.Create(Deactivate);

		displays = new BehaviorSubject<Task<string[]>>(Task.FromResult(Array.Empty<string>()));

		var isWorkingObservable = displays
			.AddTaskCompletion()
			.Select(t => !t.IsCompleted);

		isWorking = isWorkingObservable
			.ToProperty(this, x => x.IsWorking);

		availableDisplays = displays
			.AddTaskCompletion()
			.Select(task =>
			{
				if (!task.IsCompleted)
				{
					return "Finding displays...";
				}
				if (task.IsFaulted || task.IsCanceled)
				{
					return "Error finding displays.";
				}
				if (task.Result.Length == 0)
				{
					return "No displays.";
				}
				return $"Displays: {string.Join(", ", task.Result)}";
			})
			.ToProperty(this, x => x.AvailableDisplays);

		outputStream.OnNext(new Run("Debug output"));
		outputStream.OnNext(SmartLineBreak.Instance);

		this.WhenAttached(disposables =>
		{
			isWorking.DisposeWith(disposables);
			availableDisplays.DisposeWith(disposables);
			ActivateCommand.DisposeWith(disposables);
			DeactivateCommand.DisposeWith(disposables);
			availableDisplays.DisposeWith(disposables);
		});
	}

	public IObservable<Inline> OutputStream => outputStream;
	public ReactiveCommand<Unit, Unit> ActivateCommand { get; }
	public ReactiveCommand<Unit, Unit> DeactivateCommand { get; }
	public string AvailableDisplays => availableDisplays.Value;
	public bool IsWorking => isWorking.Value;

	/// <inheritdoc />
	ViewModelActivator IActivatableViewModel.Activator { get; } = new ();

	/// <inheritdoc />
	public LifecycleActivator Activator { get; } = new ();

	/// <remarks>
	///     This has to be manually called (as opposed to just calling it from the constructor) because the view model can be
	///     used from the design preview.
	/// </remarks>
	public void RefreshAvailableDisplays()
	{
		if (source == null) return;
		displays.OnNext(source.GetDisplayDevices());
	}

	private void Activate()
	{
		if (source == null) return;
		MiniLog result = source.DisableNonPrimaryDisplays();
		result.AdaptForControl().Subscribe(outputStream).Dispose();
	}

	private void Deactivate()
	{
		if (source == null) return;
		MiniLog result = source.RestoreSettings();
		result.AdaptForControl().Subscribe(outputStream).Dispose();
	}
}