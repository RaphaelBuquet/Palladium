using System.Collections.Concurrent;
using System.Collections.Specialized;
using LogViewer.Core;

namespace Palladium.Logging;

public class LogToFile : IDisposable
{
	private readonly BlockingCollection<LogModel> blockingCollection;

	public LogToFile(Log target, string filePath)
	{
		blockingCollection = new BlockingCollection<LogModel>();

		target.DataStore.Entries.CollectionChanged += EntriesOnCollectionChanged;

		var thread = new Thread(() =>
		{
			// reset file
			Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? throw new ArgumentException($"Invalid path: {filePath}"));
			File.Create(filePath).Dispose();

			while (!blockingCollection.IsCompleted)
			{
				if (!blockingCollection.TryTake(out LogModel? firstItem, TimeSpan.FromMilliseconds(-1)))
				{
					continue; // this should be the end, so the while will stop there.
				}

				// whenever an item is emitted, keep the stream open and wait a bit for log messages to come through
				using FileStream stream = File.Open(filePath, FileMode.Append, FileAccess.Write);
				using var writer = new StreamWriter(stream);
				WriteToStream(writer, firstItem);
				while (blockingCollection.TryTake(out LogModel? item, TimeSpan.FromMilliseconds(50)))
				{
					WriteToStream(writer, item);
				}

				// close the stream and go back to waiting for a message.
			}
		});
		thread.Start();
	}

	private static void WriteToStream(StreamWriter stream, LogModel item)
	{
		stream.WriteLine(AsString(item));
	}

	/// <inheritdoc />
	public void Dispose()
	{
		blockingCollection.CompleteAdding();
		blockingCollection.Dispose();
	}

	private void EntriesOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		if (!blockingCollection.IsAddingCompleted)
		{
			if (e.NewItems is null) return;
			foreach (LogModel item in e.NewItems)
			{
				blockingCollection.TryAdd(item);
			}
		}
	}

	public static string AsString(LogModel item)
	{
		string? message = string.IsNullOrEmpty(item.Exception) ? item.State?.ToString() : $"{item.State}{Environment.NewLine}Exception: {item.Exception}";
		return $"[{item.Timestamp}] |{item.LogLevel}| {message}";
	}
}