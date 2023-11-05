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
			var combinedObservable = this.WhenAnyValue(
					x => x.BrowserArguments,
					x => x.BrowserPath,
					x => x.EnableOnAppStart)
				.Skip(1); // skip initial value to only get user-driven changes

			combinedObservable
				.ObserveOn(writeToSettingsScheduler)
				.Subscribe(_ => { settingsService?.WriteCommand.Execute().Subscribe(); })
				.DisposeWith(disposables);

			combinedObservable
				.Subscribe(_ =>
				{
					Data.OnNext(new SearchOverrideSettings
					{
						BrowserArguments = browserArguments,
						BrowserPath = browserPath,
						EnableOnAppStart = enableOnAppStart
					});
				}).DisposeWith(disposables);

			Data.Subscribe(x =>
			{
				BrowserArguments = x.BrowserArguments ?? "";
				BrowserPath = x.BrowserPath ?? "";
				EnableOnAppStart = x.EnableOnAppStart;
			});
		});
	}

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
	public ViewModelActivator Activator { get ; } = new ();

	/// <inheritdoc />
	Guid ISettings<SearchOverrideSettings>.SettingsGuid => SearchOverrideAction.Guid;

	/// <inheritdoc />
	public SettingsText SettingsText { get; }

	/// <inheritdoc />
	public BehaviorSubject<SearchOverrideSettings> Data { get; } = new (new SearchOverrideSettings());

	/// <inheritdoc />
	public Subject<SearchOverrideSettings> DeserializedData { get; } = new ();
}