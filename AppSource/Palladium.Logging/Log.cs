using System.Drawing;
using LogViewer.Core;
using Microsoft.Extensions.Logging;

namespace Palladium.Logging;

public static class Log
{
	public static readonly LogDataStore DataStore = new ();

	private static readonly DataStoreLoggerConfiguration Config = new();

	static Log()
	{
		Config.Colors[LogLevel.Trace] = new LogEntryColor
		{
			Foreground = Color.Black,
			Background = Color.WhiteSmoke
		};
		Config.Colors[LogLevel.Debug] = new LogEntryColor
		{
			Foreground = Color.Black,
			Background = Color.WhiteSmoke
		};
		Config.Colors[LogLevel.Information] = new LogEntryColor
		{
			Foreground = Color.Black,
			Background = Color.White
		};
		Config.Colors[LogLevel.Warning] = new LogEntryColor
		{
			Foreground = Color.White,
			Background = Color.DarkSalmon
		};
		Config.Colors[LogLevel.Error] = new LogEntryColor
		{
			Foreground = Color.White,
			Background = Color.Crimson
		};
	}

	public static void Emit(EventId eventId, LogLevel logLevel, string message, Exception? exception = null)
	{
		// // check if we are logging for passed log level
		// if (!IsEnabled(logLevel))
		// 	return;

		DataStore.AddEntry(new LogModel
		{
			Timestamp = DateTime.UtcNow,
			LogLevel = logLevel,
			// do we override the default EventId if it exists?
			EventId = eventId.Id == 0 && Config.EventId != 0 ? Config.EventId : eventId,
			State = message,
			Exception = exception?.Message ?? (logLevel == LogLevel.Error ? message ?? "" : ""),
			Color = Config.Colors[logLevel]
		});
	}
}