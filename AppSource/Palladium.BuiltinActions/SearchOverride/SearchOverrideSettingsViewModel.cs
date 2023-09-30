using System.Reactive.Disposables;
using System.Reactive.Linq;
using Palladium.ActionsService;
using Palladium.Settings;
using ReactiveUI;

namespace Palladium.BuiltinActions.SearchOverride;

public class SearchOverrideSettingsViewModel : ReactiveObject, IActivatableViewModel, ISettings<SearchOverrideSettings>
{
	private IDisposable? dataSubscription;
	private string browserPath = "";
	private string browserArguments = "";

	public SearchOverrideSettingsViewModel() : this(null, null)
	{ }

	public SearchOverrideSettingsViewModel(ActionDescription? actionDescription, SettingsService? settingsService)
	{
		SettingsText = SettingsText.FromActionDescription(actionDescription);
		this.WhenActivated(disposables =>
		{
			Disposable.Create(() => dataSubscription?.Dispose()).DisposeWith(disposables);

			this.WhenAnyValue(x => x.BrowserPath, x => x.BrowserArguments)
				.Skip(1) // skip initial value
				.Subscribe(_ => { settingsService?.WriteCommand.Execute().Subscribe(); })
				.DisposeWith(disposables);
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
			BrowserArguments = BrowserArguments
		};
	}
}