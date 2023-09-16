using System.Reactive.Disposables;
using System.Reactive.Subjects;
using Palladium.Settings;
using ReactiveUI;

namespace Palladium.BuiltinActions.SearchOverride;

public class SearchOverrideSettingsViewModel : ReactiveObject, IActivatableViewModel, IActionSettingsViewModel<SearchOverrideSettings>
{
	private ObservableAsPropertyHelper<string>? browserPathHelper;
	private ObservableAsPropertyHelper<string>? browserArgumentsHelper;

	public readonly ReplaySubject<string> BrowserPathSubject = new (1);
	public readonly ReplaySubject<string> BrowserArgumentsSubject = new (1);

	private IDisposable? dataSubscription;

	public SearchOverrideSettingsViewModel()
	{
		this.WhenActivated(disposables =>
		{
			Disposable.Create(() => dataSubscription?.Dispose()).DisposeWith(disposables);

			browserPathHelper = BrowserPathSubject
				.ToProperty(this, x => x.BrowserPath)
				.DisposeWith(disposables);
			browserArgumentsHelper = BrowserArgumentsSubject
				.ToProperty(this, x => x.BrowserPath)
				.DisposeWith(disposables);
		});
	}

	public string BrowserPath => browserPathHelper?.Value ?? "";
	public string BrowserArguments => browserArgumentsHelper?.Value ?? "";

	/// <inheritdoc />
	ViewModelActivator IActivatableViewModel.Activator { get; } = new ();

	/// <inheritdoc />
	Guid IActionSettingsViewModel<SearchOverrideSettings>.ActionGuid => SearchOverrideAction.Guid;

	/// <inheritdoc />
	void IActionSettingsViewModel<SearchOverrideSettings>.ProcessDataObservable(IObservable<SearchOverrideSettings> observable)
	{
		dataSubscription?.Dispose();
		dataSubscription = observable.Subscribe(settings =>
		{
			BrowserPathSubject.OnNext(settings.BrowserPath);
			BrowserArgumentsSubject.OnNext(settings.BrowserArguments);
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