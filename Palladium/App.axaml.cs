using System;
using System.Reactive;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using LogViewer.Core.ViewModels;
using Microsoft.Extensions.Logging;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Palladium.Logging;
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
			
			// main window
			var mainWindow = new MainWindow();
			mainWindow.DataContext = new MainWindowViewModel(logVm, mainWindow);
			desktop.MainWindow = mainWindow;
			
			try
			{
				LogStartingMode();

				desktop.MainWindow = mainWindow;
			}
			catch (Exception ex)
			{
				ShowMessageBox("Unhandled Error", ex.Message);
				return;
			}
		}

		base.OnFrameworkInitializationCompleted();
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