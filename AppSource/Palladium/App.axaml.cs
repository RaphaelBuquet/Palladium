using System;
using System.IO;
using System.Reactive;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DynamicData;
using LogViewer.Core.ViewModels;
using Microsoft.Extensions.Logging;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Palladium.ActionsService;
using Palladium.Builtin.ImmersiveGame;
using Palladium.Builtin.SearchOverride;
using Palladium.Builtin.Settings;
using Palladium.Controls;
using Palladium.Plugins;
using Palladium.Logging;
using Palladium.Settings;
using Palladium.ViewModels;
using Palladium.Views;
using ReactiveUI;

namespace Palladium;

public  class App : Application
{
	private Log? log;
	private LogViewerControlViewModel? logVm;
	private Action? createMainWindow;

	public override void Initialize()
	{
		AvaloniaXamlLoader.Load(this);
	}

	public override void OnFrameworkInitializationCompleted()
	{
		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			// logging
			InstallLogging();

			ParseArgs(desktop, out bool minimised);

			LoadApp(desktop);
			if (!minimised)
			{
				CreateMainWindow(desktop);
			}
		}

		base.OnFrameworkInitializationCompleted();
	}

	private void LoadApp(IClassicDesktopStyleApplicationLifetime desktop)
	{
		// services
		string settingsFilePath = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
			"Palladium",
			"Settings.xml");
		var settingsService = new SettingsService(log, settingsFilePath);
		var actionsRepositoryService = new ActionsRepositoryService();
		var tabsService = new TabsService();

		// delay creation of main window
		createMainWindow = () =>
		{
			var mainWindow = new MainWindow();
			mainWindow.DataContext = new MainWindowViewModel(logVm, mainWindow, actionsRepositoryService, tabsService, settingsService);
			desktop.MainWindow = mainWindow;

			// home
			var homeViewModel = new HomeViewModel(actionsRepositoryService, tabsService);
			// the TabItem cannot be set in XAML because setting items in the XAML and binding to an ItemsSource at the same time is not supported.
			tabsService.Tabs.Add(new ApplicationTabItem()
			{
				Header = "🏠 Home",
				AllowClose = false,
				Content = new Home()
				{
					DataContext = homeViewModel
				}
			});
		};

		// background load
		Task.Run(() =>
		{
			try
			{
				InstallActions(actionsRepositoryService, log, settingsService);

				var appSettingsViewModel = new AppSettingsViewModel(new WindowsShortcutHandler(), log);
				settingsService.Install(appSettingsViewModel, () => new AppSettingsView { DataContext = appSettingsViewModel }, false);

				LogStartingMode();

				PluginsLoader.LoadAll(actionsRepositoryService, log, settingsService);
			}
			catch (Exception e)
			{ 
				log?.Emit(new EventId(), LogLevel.Critical, "Fatal error occured when booting application", e);
				desktop.Shutdown();
			}
		});
	}

	private static void ParseArgs(IClassicDesktopStyleApplicationLifetime desktop, out bool startMinimised)
	{
		startMinimised = false;

		// note: don't use LINQ as this needs maximum performance (app startup) 
		if (desktop.Args == null) return;
		foreach (string arg in desktop.Args)
		{
			if (arg.Contains(AppSettingsViewModel.StartMinimisedArgs, StringComparison.OrdinalIgnoreCase))
			{
				startMinimised = true;
			}
		}
	}

	private void InstallActions(ActionsRepositoryService actionsRepositoryService, Log? log, SettingsService settingsService)
	{
		new SearchOverrideAction().Init(actionsRepositoryService, settingsService, log);
		actionsRepositoryService.Actions.AddOrUpdate(new ImmersiveGameAction(log).Description);
	}

	private void InstallLogging()
	{
		// catch all unhandled errors
		AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
		TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
		RxApp.DefaultExceptionHandler = Observer.Create<Exception>(OnUnhandledRxException);

		// For debugging purposes only. Register the Random Logging Service
		// builder.AddRandomBackgroundService();

		// visual debugging tools
		log = new Log();
		logVm = new LogViewerControlViewModel(log.DataStore);

		// Microsoft Logger
		// builder.Logging.AddDefaultDataStoreLogger();
	}

	private void CreateMainWindow(IClassicDesktopStyleApplicationLifetime desktop)
	{
		if (desktop.MainWindow == null && createMainWindow != null)
		{
			createMainWindow?.Invoke();
			createMainWindow = null;
		}
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

	private void TrayIcon_OnClicked(object? sender, EventArgs e)
	{
		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			if (desktop.MainWindow == null)
			{
				CreateMainWindow(desktop);
			}
			else
			{
				desktop.MainWindow.Show();
				if (desktop.MainWindow.WindowState == WindowState.Minimized)
				{
					desktop.MainWindow.WindowState = WindowState.Normal;
				}
			}
		}
	}

	private void TrayIcon_Exit_OnClicked(object? sender, EventArgs e)
	{
		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			desktop.TryShutdown();
		}
	}
}