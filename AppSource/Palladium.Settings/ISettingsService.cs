using System.Reactive;
using DynamicData;
using ReactiveUI;

namespace Palladium.Settings;

public interface ISettingsService
{
	ReactiveCommand<Unit, Unit> WriteCommand { get ; }
	SourceCache<SettingsEntry, Guid> SettingsViews { get ; }

	/// <summary>
	///     Install a view and a view model for an action's settings.
	/// </summary>
	/// <param name="settings">The view model for the settings page.</param>
	/// <param name="createView">
	///     A callback that creates the view for the settings page. The invocation of this callback is
	///     delayed for performance reasons.
	/// </param>
	/// <param name="tryReadExistingSettings">
	///     If true, will try to read any existing settings saved in user preferences. The
	///     existing settings will be emitted through the observable in
	///     <see cref="ISettings{T}.ProcessDataObservable" />.
	/// </param>
	/// <returns>
	///     A task for the deserialization of existing settings. This is useful to know if the settings are being deserialized.
	///     You should implement <see cref="ISettings{T}.ProcessDataObservable" /> to get the deserialized
	///     value.
	/// </returns>
	Task Install<T> (ISettings<T> settings, Func<object> createView, bool tryReadExistingSettings);
}