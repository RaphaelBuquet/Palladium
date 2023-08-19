using System;
using System.Drawing;
using System.Reactive;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using LogViewer.Avalonia;
using LogViewer.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using MsLogger.Core;
using Palladium.ViewModels;
using Palladium.Views;
using RandomLogging.Service;
using ReactiveUI;
using Icon = MsBox.Avalonia.Enums.Icon;

namespace Palladium;

public  class App : Application
{
	private IHost? host;
	private CancellationTokenSource? cancellationTokenSource;

	public override void Initialize()
	{
		AvaloniaXamlLoader.Load(this);
	}

	public override void OnFrameworkInitializationCompleted()
	{
		HostApplicationBuilder builder = Host.CreateApplicationBuilder();

		InstallLogging(builder);

		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			IServiceCollection services = builder.Services;
            
			services
				.AddSingleton<MainWindow>()
				.AddSingleton<MainWindowViewModel>();
			
			host = builder.Build();
			cancellationTokenSource = new CancellationTokenSource();

			try
			{
				LogStartingMode();

				// set and show
				var mainWindow = host.Services.GetRequiredService<MainWindow>();
				mainWindow.DataContext = host.Services.GetRequiredService<MainWindowViewModel>();
				
				desktop.MainWindow = mainWindow;
				desktop.ShutdownRequested += OnShutdownRequested;

				// startup background services
				_ = host.StartAsync(cancellationTokenSource.Token);
			}
			catch (OperationCanceledException)
			{
				// skip
			}
			catch (Exception ex)
			{
				ShowMessageBox("Unhandled Error", ex.Message);
				return;
			}
		}

		base.OnFrameworkInitializationCompleted();
	}

	private void InstallLogging(HostApplicationBuilder builder)
	{
		// catch all unhandled errors
		AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
		TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
		RxApp.DefaultExceptionHandler = Observer.Create<Exception>(OnUnhandledRxException);
		

		// For debugging purposes only. Register the Random Logging Service
		// builder.AddRandomBackgroundService();

		// visual debugging tools
		builder.AddLogViewer();

		// Microsoft Logger
		// builder.Logging.AddDefaultDataStoreLogger();

		// colours
		builder .Logging.AddDefaultDataStoreLogger(options =>
		{
			options.Colors[LogLevel.Trace] = new LogEntryColor
			{
				Foreground = Color.Black,
				Background = Color.WhiteSmoke
			};
			options.Colors[LogLevel.Debug] = new LogEntryColor
			{
				Foreground = Color.Black,
				Background = Color.WhiteSmoke
			};
			options.Colors[LogLevel.Information] = new LogEntryColor
			{
				Foreground = Color.Black,
				Background = Color.White
			};
			options.Colors[LogLevel.Warning] = new LogEntryColor
			{
				Foreground = Color.White,
				Background = Color.DarkSalmon
			};
			options.Colors[LogLevel.Error] = new LogEntryColor
			{
				Foreground = Color.White,
				Background = Color.Crimson
			};
		});
	}

	private void OnShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
	{
		_ = host!.StopAsync(cancellationTokenSource!.Token);
	}

	private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
	{
		var logger = host!.Services.GetRequiredService<ILogger<App>>();
		var eventId = new EventId(0, Assembly.GetEntryAssembly()!.GetName().Name);
		logger.Emit(eventId, LogLevel.Error, "Unobserved Task Exception", (Exception)e.ExceptionObject);

		// show user
		ShowMessageBox("Unhandled Error", ((Exception)e.ExceptionObject).Message);
	}
	
	private void OnUnhandledRxException(Exception e)
	{
		var logger = host!.Services.GetRequiredService<ILogger<App>>();
		var eventId = new EventId(0, Assembly.GetEntryAssembly()!.GetName().Name);
		logger.Emit(eventId, LogLevel.Error, "Unobserved Task Exception", e);
		
		// show user
		ShowMessageBox("Unhandled Error", e.Message);
	}
	
	private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
	{
		var logger = host!.Services.GetRequiredService<ILogger<App>>();
		var eventId = new EventId(0, Assembly.GetEntryAssembly()!.GetName().Name);
		logger.Emit(eventId, LogLevel.Error, "Unobserved Task Exception", e.Exception);
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
		var logger = host!.Services.GetRequiredService<ILogger<App>>();
		var eventId = new EventId(0, Assembly.GetEntryAssembly()!.GetName().Name);

		// // For debugging purposes only. Log a test pattern for each log level
		// logger.TestPattern(eventId);

		// log that we have started...
		logger.Emit(eventId, LogLevel.Information, $"Running in {(isDevelopment ? "Debug" : "Release")} mode");
	}
}