using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using Palladium.Logging;
using Palladium.ObservableExtensions;
using Palladium.Settings;
using ReactiveUI;
using ReactiveUI.Validation.Abstractions;
using ReactiveUI.Validation.Contexts;
using ReactiveUI.Validation.Extensions;
using ReactiveUI.Validation.Helpers;

namespace Palladium.Builtin.Settings;

public class AppSettingsViewModel : ReactiveValidationObject, IActivatableViewModel, ISettings<AppSettings>
{
	private IDisposable? dataSubscription;

	private bool launchAtStartup;
	private bool launchAtStartupIsChanging;

	private bool hasBeenActivated = false;
	private bool blockChanges = false;

	public AppSettingsViewModel() : this(null, null)
	{ }

	/// <inheritdoc />
	public AppSettingsViewModel(IShortcutHandler? shortcutHandler, Log? log)
	{
		if (shortcutHandler != null)
		{
			CreateStartupShortcut = ReactiveCommand.CreateFromTask(shortcutHandler.CreateStartupShortcut);
			DoesStartupShortcutExist = ReactiveCommand.CreateFromTask(shortcutHandler.DoesStartupShortcutExist);
			RemoveStartupShortcut = ReactiveCommand.CreateFromTask(shortcutHandler.RemoveStartupShortcut);
		}
		else
		{
			DoesStartupShortcutExist = ReactiveCommand.Create(() => false);
			CreateStartupShortcut = ReactiveCommand.Create(delegate {  });
			RemoveStartupShortcut = ReactiveCommand.Create(delegate {  });
		}

		this.WhenActivated(disposables =>
		{
			// watch commands
			Observable.CombineLatest( CreateStartupShortcut.IsExecuting, DoesStartupShortcutExist.IsExecuting, RemoveStartupShortcut.IsExecuting,
					(b1, b2, b3) => b1 || b2 || b3)
				.BindTo(this, x => x.LaunchAtStartupIsChanging)
				.DisposeWith(disposables);

			var control = new Control();

			// watch commands errors
			// log exceptions
			CreateStartupShortcut.ThrownExceptions.LogExceptions(log).DisposeWith(disposables);
			DoesStartupShortcutExist.ThrownExceptions.LogExceptions(log).DisposeWith(disposables);
			RemoveStartupShortcut.ThrownExceptions.LogExceptions(log).DisposeWith(disposables);
			// display error to use on exception
			var errorMessages = Observable.Merge(
				CreateStartupShortcut.ThrownExceptions.Select(_ => "Failed to enable launching the app at startup."),
				CreateStartupShortcut.Select(_ => (string?)null),
				RemoveStartupShortcut.ThrownExceptions.Select(_ => "Failed to disable launching app at startup."),
				RemoveStartupShortcut.Select(_ => (string?)null),
				DoesStartupShortcutExist.ThrownExceptions.Select(_ => "Failed to retrieve current settings."),
				DoesStartupShortcutExist.Select(_ => (string?)null)
			);
			this.ValidationRule(
					x => x.LaunchAtStartup,
					errorMessages,
					error => error == null,
					error => error!)
				.DisposeWith(disposables);
			// update UI state on exception
			CreateStartupShortcut.ThrownExceptions
				.Subscribe(_ => SetLaunchAtStartupWithoutChangingShortcut(false))
				.DisposeWith(disposables);
			RemoveStartupShortcut.ThrownExceptions
				.Subscribe(_ => SetLaunchAtStartupWithoutChangingShortcut(true))
				.DisposeWith(disposables);
			
			// bind command result 
			DoesStartupShortcutExist
				.Subscribe(SetLaunchAtStartupWithoutChangingShortcut)
				.DisposeWith(disposables);

			this.WhenAnyValue(x => x.LaunchAtStartup)
				.Skip(1) // skip initial property value
				.Subscribe(newValue =>
				{
					if (blockChanges) return;
					if (newValue) CreateStartupShortcut.Execute().Subscribe();
					else RemoveStartupShortcut.Execute().Subscribe();
				}).DisposeWith(disposables);

			Disposable.Create(() => dataSubscription?.Dispose()).DisposeWith(disposables);

			if (!hasBeenActivated)
			{
				hasBeenActivated = true;
				DoesStartupShortcutExist.Execute().Subscribe();
			}
		});
	}

	public ReactiveCommand<Unit, bool> DoesStartupShortcutExist { get; }
	public ReactiveCommand<Unit, Unit> CreateStartupShortcut { get; }
	public ReactiveCommand<Unit, Unit> RemoveStartupShortcut { get; }

	/// <summary>
	///     Note: this isn't saved in user settings. Instead, it is controlled by whether the startup shortcut exists.
	/// </summary>
	public bool LaunchAtStartup
	{
		get => launchAtStartup;
		set => this.RaiseAndSetIfChanged(ref launchAtStartup, value);
	}

	/// <summary>
	///     Represents a write (create shortcut) or read (check if shortcut exists) task.
	/// </summary>
	public bool LaunchAtStartupIsChanging
	{
		get => launchAtStartupIsChanging;
		set => this.RaiseAndSetIfChanged(ref launchAtStartupIsChanging, value);
	}

	/// <inheritdoc />
	public Guid SettingsGuid { get; } = new ("2B093852-ECC9-4026-BE8C-7D2421F71492");

	/// <inheritdoc />
	public SettingsText SettingsText { get; } = new()  { Title = "Application Settings", SectionTitle = "⚙️ Application Settings" };

	/// <inheritdoc />
	ViewModelActivator IActivatableViewModel.Activator { get; } = new();

	/// <inheritdoc />
	public void ProcessDataObservable(IObservable<AppSettings> observable)
	{
		dataSubscription?.Dispose();
		dataSubscription = observable
			.Subscribe(settings => { });
	}

	/// <inheritdoc />
	public AppSettings GetDataToSerialize()
	{
		return new AppSettings();
	}

	private void SetLaunchAtStartupWithoutChangingShortcut(bool newValue)
	{
		blockChanges = true;
		LaunchAtStartup = newValue;
		blockChanges = false;
	}
}