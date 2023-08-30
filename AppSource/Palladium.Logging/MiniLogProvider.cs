using System.Reactive.Subjects;

namespace Palladium.Logging;

/// <summary>
///     Helper object to generate a <see cref="MiniLog" />.
/// </summary>
public readonly struct MiniLogProvider
{
	public readonly ReplaySubject<MiniLog.Entry> Entries;
	public readonly ReplaySubject<bool> Result;
	public readonly MiniLog Value;

	public MiniLogProvider()
	{
		Entries = new ReplaySubject<MiniLog.Entry>(1);
		Result = new ReplaySubject<bool>(1);
		Value = new MiniLog(Entries, Result);
	}
}