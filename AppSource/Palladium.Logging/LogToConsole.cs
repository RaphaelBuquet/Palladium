using System.Collections.Specialized;
using LogViewer.Core;
using Microsoft.Extensions.Logging;

namespace Palladium.Logging;

public class LogToConsole
{
	public LogToConsole(Log target)
	{
		target.DataStore.Entries.CollectionChanged += EntriesOnCollectionChanged;
	}

	private void EntriesOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		if (e.NewItems is null) return;
		foreach (LogModel item in e.NewItems)
		{
			if (item.LogLevel == LogLevel.Error)
			{
				Console.Error.WriteLineAsync(LogToFile.AsString(item));
			}
			else
			{
				Console.Out.WriteLineAsync(LogToFile.AsString(item));
			}
		}
	}
}