using Microsoft.Extensions.Logging;

namespace Palladium.Logging;

public readonly struct MiniLog
{
	public struct Entry
	{
		public LogLevel LogLevel;
		public string Message;
	}

	public readonly IObservable<Entry> Entries;
	public readonly IObservable<bool> Success;

	public MiniLog(IObservable<Entry> entries, IObservable<bool> success)
	{
		Entries = entries;
		Success = success;
	}
}