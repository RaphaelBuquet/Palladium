using System.Reactive.Subjects;

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
	///     <para>Observable to get the settings and get notified of changes.</para>
	///     <para>
	///         The settings system used this. Firstly, a value is emitted by the settings system when it deserializes
	///         settings, if an existing entry is found. Secondly, when settings need to be serialized to disk, the current
	///         value is fetched and serialized.
	///     </para>
	///     <para>
	///         Your view models properties need to have a "two-way" binding with this.
	///     </para>
	/// </summary>
	BehaviorSubject<T> Data { get; }

	/// <summary>
	///     <para>
	///         The settings system emits a value on this observable when it deserializes settings, if an existing entry is
	///         found. This is separate from <see cref="Data" /> as it can be useful to separate data changes performed by
	///         users and data changes performed when loading the settings.
	///     </para>
	///     <para>
	///         This does not emit when the user changes the values.
	///     </para>
	/// </summary>
	/// <remarks>
	///     The value will be emitted here and then on <see cref="Data" />.
	/// </remarks>
	Subject<T> DeserializedData { get; }
}