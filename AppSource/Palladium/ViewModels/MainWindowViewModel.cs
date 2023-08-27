using System;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Disposables;
using Avalonia.Controls;
using LogViewer.Core.ViewModels;
using Palladium.Views;
using ReactiveUI;

namespace Palladium.ViewModels;

public class MainWindowViewModel : ReactiveObject, IActivatableViewModel
{
	// dependencies
	private readonly LogViewerControlViewModel? logVm;
	private readonly MainWindow? mainWindow;

	private Window? logWindow;

	/// <summary>
	///     XAML preview constructor.
	/// </summary>
	public MainWindowViewModel() : this(null, null)
	{ }

	/// <inheritdoc />
	public MainWindowViewModel(LogViewerControlViewModel? logVm, MainWindow? mainWindow)
	{
		this.logVm = logVm;
		this.mainWindow = mainWindow;
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
		throw new NotImplementedException();
	}
	
	private void Debug()
	{
		Debugger.Break();
	}
}