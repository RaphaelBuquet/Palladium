using System.Diagnostics;
using System.Reactive;
using System.Reactive.Disposables;
using Avalonia.Controls.Documents;
using Palladium.Controls;
using Palladium.ObservableExtensions;
using Palladium.ObservableExtensions.Lifecycle;
using ReactiveUI;

namespace Palladium.BuiltinActions.SearchOverride;

public class SearchOverrideViewModel : ReactiveObject, IActivatableViewModel, ILifecycleAwareViewModel
{
	private readonly ReplayFirstValuesSubject<Inline> outputStream = new (2);
	private readonly WindowsKeyboard windowsKeyboard = new ();

	/// <inheritdoc />
	public SearchOverrideViewModel()
	{
		ActivateCommand = ReactiveCommand.Create(Activate);
		DeactivateCommand = ReactiveCommand.Create(Deactivate);

		outputStream.OnNext(new Run("Debug output"));
		outputStream.OnNext(SmartLineBreak.Instance);

		this.WhenAttached(disposables =>
		{
			Disposable.Create(windowsKeyboard.UnsetHook).DisposeWith(disposables);
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
			outputStream.OnNext(SmartLineBreak.Instance);

			// // firefox
			// var psi = new ProcessStartInfo()
			// {
			// 	FileName = @"C:\Program Files\Mozilla Firefox\firefox.exe",
			// 	// Arguments = "--url about:newtab",
			// };
			// Process.Start(psi);
			
            // edge
			Process.Start(@"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe", "chrome-search://local-ntp/local-ntp.html");
		}, RxApp.MainThreadScheduler, WindowsKeyboard.VK_S, WindowsKeyboard.VK_LWIN);

		outputStream.OnNext(new Run("Activated"));
		outputStream.OnNext(SmartLineBreak.Instance);
	}

	~SearchOverrideViewModel()
	{
		windowsKeyboard.UnsetHook();
	}
}