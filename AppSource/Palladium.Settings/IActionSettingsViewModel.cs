namespace Palladium.Settings;

public interface IActionSettingsViewModel<T>
{
	/// <summary>
	///     Maps settings to an action.
	/// </summary>
	Guid ActionGuid { get; }

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