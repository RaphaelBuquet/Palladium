using System.Collections.Concurrent;
using System.Reactive;
using System.Reactive.Subjects;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using DynamicData;
using Microsoft.Extensions.Logging;
using Palladium.Logging;
using ReactiveUI;

namespace Palladium.Settings;

public class SettingsService
{
	private readonly ConcurrentDictionary<Guid, SettingsSerializer> serializers = new ();
	private readonly Log? log;
	private readonly string path;
	private readonly SemaphoreSlim semaphore; // note: cannot use ReaderWriterLockSlim as it can cause deadlocks because of how async/await is used.

	public SettingsService(Log? log, string path)
	{
		this.log = log;
		this.path = path;
		semaphore = new SemaphoreSlim(1, 1);
		var canExecute = new BehaviorSubject<bool>(true);
		WriteCommand = ReactiveCommand.CreateFromTask(Write, canExecute);
		WriteCommand.ThrownExceptions.Subscribe(e =>
		{
			canExecute.OnNext(false);
			this.log?.Emit(new EventId(), LogLevel.Error, "Failed to write settings.", e);
		});
	}

	public ReactiveCommand<Unit, Unit> WriteCommand { get ; }

	public SourceCache<SettingsEntry, Guid> SettingsViews { get; } = new (pair => pair.Guid);

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
	public Task Install<T> (ISettings<T> settings, Func<object> createView, bool tryReadExistingSettings)
	{
		return Task.Run(async () =>
		{
			try
			{
				await InstallImplementation(settings, createView, tryReadExistingSettings);
			}
			catch (Exception e)
			{
				log?.Emit(new EventId(), LogLevel.Error, "An error occured when installing settings.", e);
			}
		});
	}

	private async Task InstallImplementation<T>(ISettings<T> settings, Func<object> createView, bool tryReadExistingSettings)
	{
		var writeFunction = delegate (XElement node) { WriteSerialize(node, settings.GetDataToSerialize()); };
		if (!serializers.TryAdd(settings.SettingsGuid, new SettingsSerializer { Type = typeof(T), SerializeFunction = writeFunction }))
		{
			log?.Emit(new EventId(), LogLevel.Warning, $"Settings with GUID {settings.SettingsGuid} were already installed, cannot install settings of type {settings.GetType().AssemblyQualifiedName}.");
			return;
		}

		var subject = new ReplaySubject<T>(1);

		if (tryReadExistingSettings)
		{
			try
			{
				await DeserializeAsync(settings.SettingsGuid, subject);
			}
			catch (Exception e)
			{
				log?.Emit(new EventId(), LogLevel.Warning, $"Failed to deserialize settings for {settings.SettingsGuid} {settings.GetType().AssemblyQualifiedName}", e);
				return;
			}
		}
		settings.ProcessDataObservable(subject);
		SettingsViews.AddOrUpdate(new SettingsEntry
		{
			Text = settings.SettingsText,
			Guid = settings.SettingsGuid,
			CreateView = createView
		});
	}

	private async Task Write(CancellationToken cancellationToken)
	{
		await semaphore.WaitAsync(cancellationToken);
		try
		{
			XDocument? document = null;
			if (File.Exists(path))
			{
				await using (FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read))
				{
					document = await ReadExistingSettings(cancellationToken, stream);
				}

				// if the contents cannot be read, rename the file so new settings can be created
				if (document == null)
				{
					string newPath = Path.Combine(Path.GetDirectoryName(path) ?? "", "Settings-Backup.xml");
					if (File.Exists(newPath))
					{
						File.Delete(newPath);
					}
					File.Move(path, newPath);
				}
			}

			if (document == null)
			{
				document = new XDocument(new XDeclaration("1.0", "utf-8", null));
				document.Add(new XElement("PalladiumSettings"));
			}

			XElement root = document.Element("PalladiumSettings")!;
			XElement actionSettingsRoot = root.GetElementOrCreate("ActionSettingsService");

			// serialize settings
			// order by GUID to make order of data in settings deterministic (useful when testing)
			foreach (var pair in serializers.OrderBy(s => s.Key))
			{
				var newElement = new XElement("ActionSettings");
				newElement.SetAttributeValue("Guid", pair.Key);
				newElement.SetAttributeValue("Type", pair.Value.Type.FullName);

				try
				{
					pair.Value.SerializeFunction.Invoke(newElement);
				}
				catch (Exception e)
				{
					log?.Emit(new EventId(), LogLevel.Error, $"Failed to serialize for {pair.Key} {pair.Value.Type.Name}", e);
				}
				actionSettingsRoot.Add(newElement);
			}

			cancellationToken.ThrowIfCancellationRequested();

			// write back
			string? directory = Path.GetDirectoryName(path);
			if (directory is not null) Directory.CreateDirectory(directory);
			await using (FileStream stream = File.Open(path, FileMode.Create, FileAccess.Write))
			{
				await using var xmlWriter = XmlWriter.Create(stream, new XmlWriterSettings { Async = true, Indent = true });
				await document.WriteToAsync(xmlWriter, cancellationToken);
			}
		}
		finally
		{
			semaphore.Release();
		}
	}

	private static IEnumerable<XElement> EnumerateActionSettings(XDocument doc)
	{
		XElement? root = doc.Element("PalladiumSettings");
		if (root == null)
		{
			return Enumerable.Empty<XElement>();
		}

		XElement actionSettingsRoot = root.GetElementOrCreate("ActionSettingsService");
		return actionSettingsRoot.Elements("ActionSettings");
	}

	private async Task<XDocument?> ReadExistingSettings(CancellationToken cancellationToken, FileStream stream)
	{
		XDocument doc;
		try
		{
			doc = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken);
		}
		catch (Exception)
		{
			return null;
		}

		// remove any settings that are recognised
		var settingsToRemove = EnumerateActionSettings(doc).Where(x =>
		{
			if (!x.TryGetGuidAttribute(out Guid guid)) return false;
			return serializers.ContainsKey(guid);
		}).ToList();

		foreach (XElement element in settingsToRemove)
		{
			element.Remove();
		}

		return doc;
	}

	private async Task DeserializeAsync<T>(Guid guid, IObserver<T> observer)
	{
		FileStream? stream = null;
		await semaphore.WaitAsync();
		try
		{
			if (!File.Exists(path))
			{
				return;
			}

			const int maxAttempts = 5;
			var attemptsCount = 0;
			var exceptions = new List<Exception>(5);
			do
			{
				try
				{
					stream = File.Open(path, FileMode.Open, FileAccess.Read);
				}
				catch (Exception e)
				{
					exceptions.Add(e);
					await Task.Delay(TimeSpan.FromSeconds(1));
				}
				attemptsCount++;
			} while (stream == null && attemptsCount < maxAttempts);

			if (stream == null)
			{
				throw new AggregateException($"Failed to read settings at \"{path}\"", exceptions);
			}
			if (exceptions.Any())
			{
				log?.Emit(new EventId(), LogLevel.Warning, $"Failed to read settings {exceptions.Count} times.", new AggregateException(exceptions));
			}
			await DeserializeAsync(stream, guid, observer);
		}
		finally
		{
			try
			{
				if (stream != null)
				{
					await stream.DisposeAsync();
				}
			}
			finally
			{
				semaphore.Release();
			}
		}
	}

	private async Task DeserializeAsync<T>(Stream stream, Guid guid, IObserver<T> observer)
	{
		XDocument doc = await XDocument.LoadAsync(stream, LoadOptions.None, CancellationToken.None);
		XElement? existingSettings = EnumerateActionSettings(doc).FirstOrDefault(e =>
		{
			if (!e.TryGetGuidAttribute(out Guid guidAttribute)) return false;
			return guidAttribute == guid;
		});
		if (existingSettings?.FirstNode == null) return;

		var serializer = new XmlSerializer(typeof(T));
		using XmlReader reader = existingSettings.FirstNode.CreateReader();
		object? value = serializer.Deserialize(reader);

		if (value is T convertedValue)
		{
			observer.OnNext(convertedValue);
		}
	}

	private static void WriteSerialize<T>(XElement xmlNode, T data)
	{
		using var stream = new MemoryStream();
		var serializer = new XmlSerializer(typeof(T));
		var xns = new XmlSerializerNamespaces();
		xns.Add(string.Empty, string.Empty);
		serializer.Serialize(stream, data, xns);
		stream.Position = 0;
		xmlNode.Add(XElement.Load(stream));
	}

	private struct SettingsSerializer
	{
		public Type Type;
		public Action<XElement> SerializeFunction;
	}
}