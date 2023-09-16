using System.Reactive;
using System.Reactive.Subjects;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using Microsoft.Extensions.Logging;
using Palladium.Logging;
using ReactiveUI;

namespace Palladium.Settings;

public class SettingsService
{
	private readonly Dictionary<Guid, SettingsSerializer> serializers = new ();
	private readonly Log log;
	private readonly string path;

	public SettingsService(Log log, string path)
	{
		this.log = log;
		this.path = path;
		WriteCommand = ReactiveCommand.CreateFromTask(Write);
	}

	public ReactiveCommand<Unit, Unit> WriteCommand { get ; }

	private async Task Write(CancellationToken cancellationToken)
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
		foreach (var pair in serializers)
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
				log.Emit(new EventId(), LogLevel.Error, $"Failed to serialize for {pair.Key} {pair.Value.Type.Name}", e);
			}
			actionSettingsRoot.Add(newElement);
		}

		cancellationToken.ThrowIfCancellationRequested();

		// write back
		await using (FileStream stream = File.Open(path, FileMode.Create, FileAccess.Write))
		{
			await using var xmlWriter = XmlWriter.Create(stream, new XmlWriterSettings { Async = true, Indent = true });
			await document.WriteToAsync(xmlWriter, cancellationToken);
		}

		// parse file
		// each existing xml node with an associated serializer will be removed from the parent xml node for this SettingsService
		// run serializers, add generated xml nodes into the parent xml node
		// write to file
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
		});

		foreach (XElement element in settingsToRemove)
		{
			element.Remove();
		}

		return doc;
	}

	/// <returns>
	///     A task for the deserialization of existing settings. This is useful to know if the settings are being deserialized.
	///     You should implement <see cref="IActionSettingsViewModel{T}.ProcessDataObservable" /> to get the deserialized
	///     value.
	/// </returns>
	public Task Install<T> (IActionSettingsViewModel<T> viewModel, bool tryReadExistingSettings)
	{
		var writeFunction = delegate (XElement node) { WriteSerialize(node, viewModel.GetDataToSerialize()); };
		serializers.Add(viewModel.ActionGuid, new SettingsSerializer { Type = typeof(T), SerializeFunction = writeFunction });

		var subject = new ReplaySubject<T>(1);

		var readTask = Task.CompletedTask;
		if (tryReadExistingSettings)
		{
			readTask = DeserializeAsync(viewModel.ActionGuid, subject);
			readTask.ContinueWith(task => { log.Emit(new EventId(), LogLevel.Warning, $"Failed to deserialize for {viewModel.ActionGuid} {typeof(T).Name}", task.Exception); }, TaskContinuationOptions.OnlyOnFaulted);
		}
		viewModel.ProcessDataObservable(subject);

		return readTask;
	}

	private async Task DeserializeAsync<T>(Guid guid, IObserver<T> observer)
	{
		if (!File.Exists(path))
		{
			return;
		}

		const int maxAttempts = 5;
		FileStream? stream = null;
		var attemptsCount = 0;
		var exceptions = new List<Exception>(5);
		try
		{
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

			if (exceptions.Any() || stream == null)
			{
				throw new AggregateException($"Failed to read settings at \"{path}\"", exceptions);
			}

			await DeserializeAsync(stream, guid, observer);
		}
		finally
		{
			if (stream != null)
			{
				await stream.DisposeAsync();
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
		// using var xmlWriter = XmlWriter.Create(new XmlTextWriter(stream, Encoding.UTF8));
		var serializer = new XmlSerializer(typeof(T));
		serializer.Serialize(stream, data);
		stream.Position = 0;
		xmlNode.Add(XElement.Load(stream));
	}

	private struct SettingsSerializer
	{
		public Type Type;
		public Action<XElement> SerializeFunction;
	}
}