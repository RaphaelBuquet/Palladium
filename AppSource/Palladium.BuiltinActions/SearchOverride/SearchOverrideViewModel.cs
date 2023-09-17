﻿using System.Diagnostics;
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
	private readonly SearchOverrideSettingsViewModel? settings;
	private readonly ReplayFirstValuesSubject<Inline> outputStream = new (2);
	private readonly WindowsKeyboard windowsKeyboard = new ();

	public SearchOverrideViewModel() : this(null)
	{ }

	/// <inheritdoc />
	public SearchOverrideViewModel(SearchOverrideSettingsViewModel? settings)
	{
		this.settings = settings;
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
			if (!string.IsNullOrWhiteSpace(settings?.BrowserPath) && File.Exists(settings.BrowserPath))
			{
				var psi = new ProcessStartInfo
				{
					FileName = settings.BrowserPath,
					Arguments = settings.BrowserArguments
				};
				Process.Start(psi);
				outputStream.OnNext(new Run($", starting {Path.GetFileName(settings.BrowserPath)}"));
			}
			outputStream.OnNext(SmartLineBreak.Instance);
		}, RxApp.MainThreadScheduler, WindowsKeyboard.VK_S, WindowsKeyboard.VK_LWIN);

		outputStream.OnNext(new Run("Activated"));
		outputStream.OnNext(SmartLineBreak.Instance);
	}

	~SearchOverrideViewModel()
	{
		windowsKeyboard.UnsetHook();
	}
}