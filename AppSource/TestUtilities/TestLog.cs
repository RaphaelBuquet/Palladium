using System.Reactive;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using LogViewer.Core;
using Palladium.Logging;

namespace TestUtilities;

public static class TestLog
{
	public static IDisposable LogToConsole(out Log log)
	{
		log = new Log();
		return log.DataStore.Entries
			.ToObservableChangeSet()
			.WhereReasonsAre(ListChangeReason.Add)
			.SelectMany(change => change.Select(i => i.Item.Current))
			.Where(changeSet => changeSet.Exception is not null)
			.Subscribe(Observer.Create<LogModel>(e => Console.WriteLine((string?)e.Exception)));
	}
}