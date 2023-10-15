using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Palladium.ExtensionFunctions;
using Palladium.Logging;
using Palladium.Settings;
using ReactiveUI;
using ReactiveUI.Validation.Extensions;
using ReactiveUI.Validation.Helpers;

namespace Palladium.Builtin.Settings;

public class AppSettingsViewModel : ReactiveValidationObject, IActivatableViewModel, ISettings<AppSettings>
{
	public const string StartMinimisedArgs = "--minimised";

	private IDisposable? dataSubscription;

	private bool launchAtStartup;
	private bool shortcutIsChanging;

	private bool hasBeenActivated = false;
	private bool blockChanges = false;

	private bool startMinimised;

	public AppSettingsViewModel() : this(null, null)
	{ }

	/// <inheritdoc />
	public AppSettingsViewModel(IShortcutHandler? shortcutHandler, Log? log)
	{
		if (shortcutHandler != null)
		{
			CreateStartupShortcut = ReactiveCommand.CreateFromTask<Shortcut>(shortcutHandler.CreateStartupShortcut);
			TryGetStartupShortcut = ReactiveCommand.CreateFromTask(shortcutHandler.TryGetStartupShortcut);
			RemoveStartupShortcut = ReactiveCommand.CreateFromTask(shortcutHandler.RemoveStartupShortcut);
		}
		else
		{
			CreateStartupShortcut = ReactiveCommand.Create<Shortcut>(delegate {  });
			TryGetStartupShortcut = ReactiveCommand.Create(() => (Shortcut?)null);
			RemoveStartupShortcut = ReactiveCommand.Create(delegate {  });
		}

		this.WhenActivated(disposables =>
		{
			// watch commands
			Observable.CombineLatest(CreateStartupShortcut.IsExecuting, TryGetStartupShortcut.IsExecuting, RemoveStartupShortcut.IsExecuting,
					(b1, b2, b3) => b1 || b2 || b3)
				.BindTo(this, x => x.ShortcutIsChanging)
				.DisposeWith(disposables);

			// watch commands errors
			// log exceptions
			CreateStartupShortcut.ThrownExceptions.LoggedCatch(log).DisposeWith(disposables);
			TryGetStartupShortcut.ThrownExceptions.LoggedCatch(log).DisposeWith(disposables);
			RemoveStartupShortcut.ThrownExceptions.LoggedCatch(log).DisposeWith(disposables);
			// display error to use on exception
			var errorMessages = Observable.Merge(
				CreateStartupShortcut.ThrownExceptions.Select(_ => "Failed to enable launching the app at startup."),
				CreateStartupShortcut.Select(_ => (string?)null),
				RemoveStartupShortcut.ThrownExceptions.Select(_ => "Failed to disable launching app at startup."),
				RemoveStartupShortcut.Select(_ => (string?)null),
				TryGetStartupShortcut.ThrownExceptions.Select(_ => "Failed to retrieve current settings."),
				TryGetStartupShortcut.Select(_ => (string?)null)
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
			TryGetStartupShortcut
				.Select(x => x.HasValue)
				.Subscribe(SetLaunchAtStartupWithoutChangingShortcut)
				.DisposeWith(disposables);
			TryGetStartupShortcut
				.Select(x => x?.Arguments?.Contains(StartMinimisedArgs, StringComparison.OrdinalIgnoreCase) == true)
				.Subscribe(SetStartMinimisedWithoutChangingShortcut)
				.DisposeWith(disposables);

			this.WhenAnyValue(x => x.LaunchAtStartup, x => x.StartMinimised)
				.Skip(1) // skip initial property value
				.Subscribe(newValues =>
				{
					if (LaunchAtStartup is false && StartMinimised is true)
					{
						StartMinimised = false;
						return; // return because setting the value above will call this code again
					}

					if (blockChanges) return;
					if (LaunchAtStartup)
					{
						CreateStartupShortcut.Execute(CreateShortcutDescription())
							.IgnoreErrors() // already handled in ThrownExceptions
							.Subscribe();
					}
					else
					{
						RemoveStartupShortcut.Execute()
							.IgnoreErrors() // already handled in ThrownExceptions
							.Subscribe();
					}
				}).DisposeWith(disposables);

			Disposable.Create(() => dataSubscription?.Dispose()).DisposeWith(disposables);

			if (!hasBeenActivated)
			{
				hasBeenActivated = true;
				TryGetStartupShortcut.Execute().Subscribe();
			}
		});
	}

	public ReactiveCommand<Unit, Shortcut?> TryGetStartupShortcut { get; }
	public ReactiveCommand<Shortcut, Unit> CreateStartupShortcut { get; }
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
	///     When true the shortcut for the app will have an argument to start the app minimised.
	///     This is faster than having to load the settings file to check if the app needs to be minimised.
	/// </summary>
	public bool StartMinimised
	{
		get => startMinimised;
		set
		{
			// cannot be set to true if LaunchAtStartup is false 
			if (!LaunchAtStartup && value)
			{
				return;
			}
			this.RaiseAndSetIfChanged(ref startMinimised, value);
		}
	}

	/// <summary>
	///     Represents a write (create shortcut) or read (check if shortcut exists) task.
	/// </summary>
	public bool ShortcutIsChanging
	{
		get => shortcutIsChanging;
		set => this.RaiseAndSetIfChanged(ref shortcutIsChanging, value);
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

	private void SetStartMinimisedWithoutChangingShortcut(bool newValue)
	{
		blockChanges = true;
		StartMinimised = newValue;
		blockChanges = false;
	}

	private Shortcut CreateShortcutDescription()
	{
		if (StartMinimised)
		{
			return new Shortcut { Arguments = StartMinimisedArgs };
		}
		return new Shortcut();
	}
}