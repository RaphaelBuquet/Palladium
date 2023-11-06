using System.Diagnostics;
using System.Reactive;
using System.Reactive.Disposables;
using Avalonia.Controls.Documents;
using Microsoft.Extensions.Logging;
using Palladium.Controls;
using Palladium.ExtensionFunctions;
using Palladium.ExtensionFunctions.Lifecycle;
using Palladium.Logging;
using ReactiveUI;

namespace Palladium.Builtin.SearchOverride;

public class SearchOverrideViewModel : ReactiveObject, IActivatableViewModel, ILifecycleAwareViewModel
{
	private readonly SearchOverrideSettingsViewModel? settingsVM;
	private readonly Log? log;
	private readonly ReplayFirstValuesSubject<Inline> outputStream = new (2);
	private readonly WindowsKeyboard windowsKeyboard = new ();

	public SearchOverrideViewModel() : this(null, null)
	{ }

	/// <inheritdoc />
	public SearchOverrideViewModel(SearchOverrideSettingsViewModel? settingsVm, Log? log)
	{
		settingsVM = settingsVm;
		this.log = log;
		ActivateCommand = ReactiveCommand.Create(Activate);
		DeactivateCommand = ReactiveCommand.Create(Deactivate);

		this.WhenAttached(disposables =>
		{
			outputStream.OnNext(new Run("Debug output"));
			outputStream.OnNext(SmartLineBreak.Instance);

			Disposable.Create(windowsKeyboard.UnsetHook).DisposeWith(disposables);
			ActivateCommand.ThrownExceptions.LoggedCatch(log, "An error occured while processing Activate").DisposeWith(disposables);
			DeactivateCommand.ThrownExceptions.LoggedCatch(log, "An error occured while processing Deactivate").DisposeWith(disposables);
			ActivateCommand.DisposeWith(disposables);
			DeactivateCommand.DisposeWith(disposables);
		});
	}

	public IObservable<Inline> OutputStream => outputStream;

	public ReactiveCommand<Unit, Unit> ActivateCommand { get; }
	public ReactiveCommand<Unit, Unit> DeactivateCommand { get; }

	/// <inheritdoc />
	ViewModelActivator IActivatableViewModel.Activator { get; } = new ();

	/// <inheritdoc />
	LifecycleActivator ILifecycleAwareViewModel.Activator { get; } = new();

	private void Deactivate()
	{
		windowsKeyboard.UnsetHook();
		outputStream.OnNext(new Run("Deactivated"));
		outputStream.OnNext(SmartLineBreak.Instance);
	}

	private void Activate()
	{
		windowsKeyboard.InstallKeyboardShortcut(() =>
		{
			outputStream.OnNext(new Run($"{DateTime.Now:HH:mm:ss.ffff} Shortcut pressed"));
			var currentSettings = settingsVM?.Data.Value;
			if (currentSettings != null && !string.IsNullOrWhiteSpace(currentSettings.Value.BrowserPath) && File.Exists(currentSettings.Value.BrowserPath))
			{
				try
				{
					var psi = new ProcessStartInfo
					{
						FileName = currentSettings.Value.BrowserPath,
						Arguments = currentSettings.Value.BrowserArguments
					};
					Process.Start(psi);
					outputStream.OnNext(new Run($", starting \"{Path.GetFileName(currentSettings.Value.BrowserPath)}\"."));
				}
				catch (Exception e)
				{
					log?.Emit(new EventId(), LogLevel.Error, $"Failed to start \"{currentSettings.Value.BrowserPath}\".", e);
					outputStream.OnNext(new Run($", failed to start \"{Path.GetFileName(currentSettings.Value.BrowserPath)}\"."));
				}
			}
			outputStream.OnNext(SmartLineBreak.Instance);
		}, RxApp.MainThreadScheduler, WindowsKeyboard.VK_S, WindowsKeyboard.VK_LWIN);

		outputStream.OnNext(new Run("Activated"));
		outputStream.OnNext(SmartLineBreak.Instance);
	}

	~SearchOverrideViewModel()
	{
		windowsKeyboard.UnsetHook();
	}
}