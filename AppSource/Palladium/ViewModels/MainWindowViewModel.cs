using System.Diagnostics;
using System.Reactive;
using System.Reactive.Disposables;
using Avalonia.Controls;
using LogViewer.Core.ViewModels;
using Palladium.ActionsService;
using Palladium.Builtin.Settings;
using Palladium.Settings;
using Palladium.Views;
using ReactiveUI;

namespace Palladium.ViewModels;

public class MainWindowViewModel : ReactiveObject, IActivatableViewModel
{
	// dependencies
	private readonly LogViewerControlViewModel? logVm;
	private readonly MainWindow? mainWindow;
	private readonly ActionsRepositoryService? actionsRepositoryService;
	private readonly TabsService? tabsService;
	private readonly SettingsService? settingsService;
	private SettingsAction? settingsAction;

	private Window? logWindow;

	/// <summary>
	///     XAML preview constructor.
	/// </summary>
	public MainWindowViewModel() : this(null, null, null, null, null)
	{ }

	/// <inheritdoc />
	public MainWindowViewModel(LogViewerControlViewModel? logVm, MainWindow? mainWindow, ActionsRepositoryService? actionsRepositoryService, TabsService? tabsService, SettingsService? settingsService)
	{
		this.logVm = logVm;
		this.mainWindow = mainWindow;
		this.actionsRepositoryService = actionsRepositoryService;
		this.tabsService = tabsService;
		this.settingsService = settingsService;
		OpenLogsCommand = ReactiveCommand.Create(OpenLogs);
		OpenSettingsCommand = ReactiveCommand.Create(OpenSettings);
		DebugCommand = ReactiveCommand.Create(Debug);

		this.WhenActivated(disposables =>
		{
			OpenLogsCommand.DisposeWith(disposables);
			OpenSettingsCommand.DisposeWith(disposables);
			DebugCommand.DisposeWith(disposables);
		});
	}

	public ReactiveCommand<Unit, Unit> OpenLogsCommand { get ; }
	public ReactiveCommand<Unit, Unit> OpenSettingsCommand { get ; }
	public ReactiveCommand<Unit, Unit> DebugCommand { get ; }

#if DEBUG
	public bool ShowDebug => true;
#else
	public bool ShowDebug => false;
#endif

	/// <inheritdoc />
	ViewModelActivator IActivatableViewModel.Activator { get; } = new ();

	private void OpenLogs()
	{
		if (logVm == null || mainWindow == null) return;

		// if window has been closed it needs to be re-created.
		if (logWindow != null && logWindow.PlatformImpl == null)
		{
			logWindow = null;
		}

		logWindow ??= new LogsWindow { DataContext = logVm };
		logWindow.Show(mainWindow);
	}

	private void OpenSettings()
	{
		if (tabsService == null || settingsService == null || actionsRepositoryService == null) return;

		if (settingsAction == null)
		{
			settingsAction = new SettingsAction();
			settingsAction.Init(settingsService);
		}
		tabsService?.HandleStartAction(settingsAction.Description);
	}

	private void Debug()
	{
		Debugger.Break();
	}
}