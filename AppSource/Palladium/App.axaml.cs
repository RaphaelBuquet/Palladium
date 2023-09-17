using System;
using System.IO;
using System.Reactive;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DynamicData;
using LogViewer.Core.ViewModels;
using Microsoft.Extensions.Logging;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Palladium.ActionsService;
using Palladium.BuiltinActions.ImmersiveGame;
using Palladium.BuiltinActions.SearchOverride;
using Palladium.Extensions;
using Palladium.Logging;
using Palladium.Settings;
using Palladium.ViewModels;
using Palladium.Views;
using ReactiveUI;

namespace Palladium;

public  class App : Application
{
	private Log? log;

	public override void Initialize()
	{
		AvaloniaXamlLoader.Load(this);
	}

	public override void OnFrameworkInitializationCompleted()
	{
		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			// logging
			InstallLogging(out LogViewerControlViewModel logVm, out log);

			// services
			string settingsFilePath = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
				"Palladium",
				"Settings.xml");
			var tabsService = new TabsService();
			var settingsService = new SettingsService(log, settingsFilePath);
			ActionsRepositoryService actionsRepositoryService = InstallActions(log, settingsService);

			// main window
			var mainWindow = new MainWindow();
			mainWindow.DataContext = new MainWindowViewModel(logVm, mainWindow, actionsRepositoryService, tabsService, settingsService);
			desktop.MainWindow = mainWindow;
			tabsService.Target = mainWindow.Tabs;

			// home
			var homeViewModel = new HomeViewModel(actionsRepositoryService, tabsService);
			mainWindow.Home.DataContext = homeViewModel;

			// background load
			Task.Run(() =>
			{
				LogStartingMode();

				new ExtensionsLoader(Path.Combine(Environment.CurrentDirectory, "Extensions"))
					.LoadExtensions(actionsRepositoryService, log);
			});
		}

		base.OnFrameworkInitializationCompleted();
	}

	private ActionsRepositoryService InstallActions(Log log, SettingsService settingsService)
	{
		var actionsRepositoryService = new ActionsRepositoryService();

		new SearchOverrideAction().Init(actionsRepositoryService, settingsService, log);
		actionsRepositoryService.Actions.AddOrUpdate(new ImmersiveGameAction(log).Description);

		return actionsRepositoryService;
	}

	private void InstallLogging(out LogViewerControlViewModel vm, out Log log)
	{
		// catch all unhandled errors
		AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
		TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
		RxApp.DefaultExceptionHandler = Observer.Create<Exception>(OnUnhandledRxException);


		// For debugging purposes only. Register the Random Logging Service
		// builder.AddRandomBackgroundService();

		// visual debugging tools
		log = new Log();
		vm = new LogViewerControlViewModel(log.DataStore);

		// Microsoft Logger
		// builder.Logging.AddDefaultDataStoreLogger();
	}

	private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
	{
		var eventId = new EventId(0, Assembly.GetEntryAssembly()!.GetName().Name);
		log?.Emit(eventId, LogLevel.Error, "Unobserved Task Exception", (Exception)e.ExceptionObject);

		// show user
		ShowMessageBox("Unhandled Error", ((Exception)e.ExceptionObject).Message);
	}

	private void OnUnhandledRxException(Exception e)
	{
		var eventId = new EventId(0, Assembly.GetEntryAssembly()!.GetName().Name);
		log?.Emit(eventId, LogLevel.Error, "Unobserved Task Exception", e);

		// show user
		ShowMessageBox("Unhandled Error", e.Message);
	}

	private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
	{
		var eventId = new EventId(0, Assembly.GetEntryAssembly()!.GetName().Name);
		log?.Emit(eventId, LogLevel.Error, "Unobserved Task Exception", e.Exception);
	}

	private void ShowMessageBox(string title, string message)
	{
		var messageBoxStandardWindow = MessageBoxManager
			.GetMessageBoxStandard(title, message, ButtonEnum.Ok, Icon.Stop);

		messageBoxStandardWindow.ShowAsync();
	}

	private void LogStartingMode()
	{
		// Get the Launch mode
		bool isDevelopment = string.Equals(Environment.GetEnvironmentVariable("DOTNET_MODIFIABLE_ASSEMBLIES"), "debug",
			StringComparison.InvariantCultureIgnoreCase);

		// initialize a logger & EventId
		var eventId = new EventId(0, Assembly.GetEntryAssembly()!.GetName().Name);

		// // For debugging purposes only. Log a test pattern for each log level
		// logger.TestPattern(eventId);

		// log that we have started...
		log?.Emit(eventId, LogLevel.Information, $"Running in {(isDevelopment ? "Debug" : "Release")} mode");
	}
}