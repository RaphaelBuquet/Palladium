using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Palladium.ActionsService;
using Palladium.Settings;
using ReactiveUI;

namespace Palladium.Builtin.SearchOverride;

public class SearchOverrideSettingsViewModel : ReactiveObject, IActivatableViewModel, ISettings<SearchOverrideSettings>
{
	private IDisposable? dataSubscription;
	private string browserPath = "";
	private string browserArguments = "";
	private bool enableOnAppStart;
	private readonly ReplaySubject<SearchOverrideSettings> loadedSettingsObservable = new (1);

	public SearchOverrideSettingsViewModel() : this(null, null)
	{ }

	public SearchOverrideSettingsViewModel(ActionDescription? actionDescription, ISettingsService? settingsService, IScheduler? writeToSettingsScheduler = null)
	{
		writeToSettingsScheduler ??= RxApp.TaskpoolScheduler; // write settings in background
		SettingsText = SettingsText.FromActionDescription(actionDescription);
		this.WhenActivated(disposables =>
		{
			Disposable.Create(() => dataSubscription?.Dispose()).DisposeWith(disposables);

			this.WhenAnyValue(
					x => x.BrowserPath,
					x => x.BrowserArguments,
					x => x.EnableOnAppStart)
				.Skip(1) // skip initial value to only get user-driven changes
				.ObserveOn(writeToSettingsScheduler)
				.Subscribe(_ => { settingsService?.WriteCommand.Execute().Subscribe(); })
				.DisposeWith(disposables);
		});
	}

	/// <summary>
	///     Emits settings loaded from the settings files. This does not emit when the user changes the values.
	/// </summary>
	public IObservable<SearchOverrideSettings> LoadedSettingsObservable => loadedSettingsObservable;

	public string BrowserPath
	{
		get => browserPath;
		set => this.RaiseAndSetIfChanged(ref browserPath, value);
	}

	public string BrowserArguments
	{
		get => browserArguments;
		set => this.RaiseAndSetIfChanged(ref browserArguments, value);
	}

	public bool EnableOnAppStart
	{
		get => enableOnAppStart;
		set => this.RaiseAndSetIfChanged(ref enableOnAppStart, value);
	}

	/// <inheritdoc />
	ViewModelActivator IActivatableViewModel.Activator { get ; } = new ();

	/// <inheritdoc />
	Guid ISettings<SearchOverrideSettings>.SettingsGuid => SearchOverrideAction.Guid;

	/// <inheritdoc />
	public SettingsText SettingsText { get; }

	/// <inheritdoc />
	void ISettings<SearchOverrideSettings>.ProcessDataObservable(IObservable<SearchOverrideSettings> observable)
	{
		dataSubscription?.Dispose();
		dataSubscription = observable
			.ObserveOn(RxApp.MainThreadScheduler)
			.Subscribe(settings =>
			{
				BrowserPath = settings.BrowserPath;
				BrowserArguments = settings.BrowserArguments;
				EnableOnAppStart = settings.EnableOnAppStart;
				loadedSettingsObservable.OnNext(settings);
			});
	}

	/// <inheritdoc />
	SearchOverrideSettings ISettings<SearchOverrideSettings>.GetDataToSerialize()
	{
		return GetCurrentSettings();
	}

	private SearchOverrideSettings GetCurrentSettings()
	{
		return new SearchOverrideSettings
		{
			BrowserPath = BrowserPath,
			BrowserArguments = BrowserArguments,
			EnableOnAppStart = EnableOnAppStart
		};
	}
}