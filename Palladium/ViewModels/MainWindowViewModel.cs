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

		this.WhenActivated(disposables =>
		{
			OpenLogsCommand.DisposeWith(disposables);
			OpenSettingsCommand.DisposeWith(disposables);
		});
	}

	public ReactiveCommand<Unit, Unit> OpenLogsCommand { get ; }
	public ReactiveCommand<Unit, Unit> OpenSettingsCommand { get ; }

	/// <inheritdoc />
	ViewModelActivator IActivatableViewModel.Activator { get; } = new ();

	private void OpenLogs()
	{
		if (logVm == null || mainWindow == null) return;
		logWindow ??= new LogsWindow { DataContext = logVm };
		logWindow.Show(mainWindow);
	}

	private void OpenSettings()
	{
		throw new System.NotImplementedException();
	}
}