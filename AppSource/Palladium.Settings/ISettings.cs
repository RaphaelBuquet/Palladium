namespace Palladium.Settings;

/// <summary>
///     Implementations of this will typically be view models, which will be used when displaying an action's settings.
/// </summary>
/// <typeparam name="T">Type of the data to serialize.</typeparam>
public interface ISettings<T>
{
	/// <summary>
	///     Identifier for these settings. Will be used when serializing and deserializing.
	/// </summary>
	Guid SettingsGuid { get; }

	/// <summary>
	///     User-facing text in the settings view. You should use <see cref="Settings.SettingsText.FromActionDescription" /> to
	///     set this.
	/// </summary>
	SettingsText SettingsText { get; }

	/// <summary>
	///     Sets the observable that emits data serialized from disk.
	///     When implementing this interface, you should subscribe to this <paramref name="observable" />.
	///     That observable will emit, upon subscription or later, the last data serialized from disk (or will never emit if
	///     there isn't any).
	/// </summary>
	/// <param name="observable"></param>
	void ProcessDataObservable(IObservable<T> observable);

	/// <summary>
	///     Called when writing data to disk.
	/// </summary>
	T GetDataToSerialize();
}