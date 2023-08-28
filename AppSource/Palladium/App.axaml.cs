using System;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
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
using Palladium.Extensions;
using Palladium.Logging;
using Palladium.ObservableExtensions;
using Palladium.ViewModels;
using Palladium.Views;
using ReactiveUI;
using Icon = MsBox.Avalonia.Enums.Icon;

namespace Palladium;

public  class App : Application
{
	public override void Initialize()
	{
		AvaloniaXamlLoader.Load(this);
	}

	public override void OnFrameworkInitializationCompleted()
	{
		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			// logging
			var logVm = InstallLogging();
			
			// services
			var tabsService = new TabsService();
			var actionsRepositoryService = InstallActions();
            
			// main window
			var mainWindow = new MainWindow();
			mainWindow.DataContext = new MainWindowViewModel(logVm, mainWindow);
			desktop.MainWindow = mainWindow;
			tabsService.Target = mainWindow.Tabs;
			
			// home
			var homeViewModel = new HomeViewModel(actionsRepositoryService, tabsService);
			mainWindow.Home.DataContext = homeViewModel;

			actionsRepositoryService.Actions.Connect()
				.Select(set =>
				{
					var change = set.ToList().First();

					var item = change.Range.Any() ? change.Range.First() : change.Item.Current;
					
					return $"{change.Reason}: {item.Title}";
				})
				.DebugToLogs("ACTIONS CHANGE")
				.Subscribe();
			
			// background load
			Task.Run(() =>
			{
				LogStartingMode();
				
				new ExtensionsLoader(Path.Combine(Environment.CurrentDirectory, "Extensions"))
					.LoadExtensions(actionsRepositoryService);
			});
		}

		base.OnFrameworkInitializationCompleted();
	}

	private ActionsRepositoryService InstallActions()
	{
		var actionsRepositoryService = new ActionsRepositoryService();

		actionsRepositoryService.Actions.Add(new ImmersiveGameAction().Description);
		
		return actionsRepositoryService;
	}

	private LogViewerControlViewModel InstallLogging()
	{
		// catch all unhandled errors
		AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
		TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
		RxApp.DefaultExceptionHandler = Observer.Create<Exception>(OnUnhandledRxException);
		

		// For debugging purposes only. Register the Random Logging Service
		// builder.AddRandomBackgroundService();

		// visual debugging tools
		var vm = new LogViewerControlViewModel(Log.DataStore);

		// Microsoft Logger
		// builder.Logging.AddDefaultDataStoreLogger();

		return vm;
	}
    
	private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
	{
		var eventId = new EventId(0, Assembly.GetEntryAssembly()!.GetName().Name);
		Log.Emit(eventId, LogLevel.Error, "Unobserved Task Exception", (Exception)e.ExceptionObject);

		// show user
		ShowMessageBox("Unhandled Error", ((Exception)e.ExceptionObject).Message);
	}
	
	private void OnUnhandledRxException(Exception e)
	{
		var eventId = new EventId(0, Assembly.GetEntryAssembly()!.GetName().Name);
		Log.Emit(eventId, LogLevel.Error, "Unobserved Task Exception", e);
		
		// show user
		ShowMessageBox("Unhandled Error", e.Message);
	}
	
	private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
	{
		var eventId = new EventId(0, Assembly.GetEntryAssembly()!.GetName().Name);
		Log.Emit(eventId, LogLevel.Error, "Unobserved Task Exception", e.Exception);
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
		Log.Emit(eventId, LogLevel.Information, $"Running in {(isDevelopment ? "Debug" : "Release")} mode");
	}
}