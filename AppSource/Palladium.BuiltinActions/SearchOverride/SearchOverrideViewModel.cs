using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using Avalonia.Controls.Documents;
using Palladium.Controls;
using Palladium.ObservableExtensions;
using Palladium.ObservableExtensions.Lifecycle;
using ReactiveUI;

namespace Palladium.BuiltinActions.SearchOverride;

public class SearchOverrideViewModel : ReactiveObject, IActivatableViewModel, ILifecycleAwareViewModel
{
	private readonly ReplayFirstValuesSubject<Inline> outputStream = new (2);

	/// <inheritdoc />
	public SearchOverrideViewModel()
	{
		ActivateCommand = ReactiveCommand.Create(Activate);
		DeactivateCommand = ReactiveCommand.Create(Deactivate);

		outputStream.OnNext(new Run("Debug output"));
		outputStream.OnNext(SmartLineBreak.Instance);

		this.WhenAttached(disposables =>
		{
			Disposable.Create(WindowsKeyboard.UnsetHook).DisposeWith(disposables);
			ActivateCommand.DisposeWith(disposables);
			DeactivateCommand.DisposeWith(disposables);
		});
	}

	public IObservable<Inline> OutputStream => outputStream;

	public ReactiveCommand<Unit, Unit> ActivateCommand { get; }
	public ReactiveCommand<Unit, Unit> DeactivateCommand { get; }

	/// <inheritdoc />
	public ViewModelActivator Activator { get; } = new ();

	/// <inheritdoc />
	LifecycleActivator ILifecycleAwareViewModel.Activator { get; } = new();

	private void Deactivate()
	{
		WindowsKeyboard.UnsetHook();
		outputStream.OnNext(new Run("Deactivated"));
		outputStream.OnNext(SmartLineBreak.Instance);
	}

	private void Activate()
	{
		WindowsKeyboard.InstallKeyboardShortcut(() =>
		{
			outputStream.OnNext(new Run("Callback!!"));
			outputStream.OnNext(SmartLineBreak.Instance);
		}, RxApp.MainThreadScheduler, WindowsKeyboard.VK_S, null, null);

		outputStream.OnNext(new Run("Activated"));
		outputStream.OnNext(SmartLineBreak.Instance);
	}

	~SearchOverrideViewModel()
	{
		WindowsKeyboard.UnsetHook();
	}
}