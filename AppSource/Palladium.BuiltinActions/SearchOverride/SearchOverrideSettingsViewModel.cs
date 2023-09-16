using System.Reactive.Disposables;
using System.Reactive.Linq;
using Palladium.Settings;
using ReactiveUI;

namespace Palladium.BuiltinActions.SearchOverride;

public class SearchOverrideSettingsViewModel : ReactiveObject, IActivatableViewModel, IActionSettingsViewModel<SearchOverrideSettings>
{
	private readonly SettingsService? settingsService;

	private IDisposable? dataSubscription;
	private string browserPath = "";
	private string browserArguments = "";

	public SearchOverrideSettingsViewModel() : this(null)
	{ }

	public SearchOverrideSettingsViewModel(SettingsService? settingsService)
	{
		this.settingsService = settingsService;
		this.WhenActivated(disposables =>
		{
			Disposable.Create(() => dataSubscription?.Dispose()).DisposeWith(disposables);

			this.WhenAnyValue(x => x.BrowserPath, x => x.BrowserArguments)
				.Skip(1) // skip initial value
				.Subscribe(_ => settingsService?.WriteCommand.Execute().Subscribe())
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
	Guid IActionSettingsViewModel<SearchOverrideSettings>.ActionGuid => SearchOverrideAction.Guid;

	/// <inheritdoc />
	void IActionSettingsViewModel<SearchOverrideSettings>.ProcessDataObservable(IObservable<SearchOverrideSettings> observable)
	{
		dataSubscription?.Dispose();
		dataSubscription = observable.Subscribe(settings =>
		{
			BrowserPath = settings.BrowserPath;
			BrowserArguments = settings.BrowserArguments;
		});
	}

	/// <inheritdoc />
	SearchOverrideSettings IActionSettingsViewModel<SearchOverrideSettings>.GetDataToSerialize()
	{
		return new SearchOverrideSettings
		{
			BrowserPath = BrowserPath,
			BrowserArguments = BrowserArguments
		};
	}
}