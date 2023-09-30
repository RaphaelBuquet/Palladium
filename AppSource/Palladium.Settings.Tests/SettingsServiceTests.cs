using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Xml;
using DynamicData;
using DynamicData.Binding;
using LogViewer.Core;
using Palladium.Logging;

namespace Palladium.Settings.Tests;

public class SettingsServiceTests
{
	[Test]
	public void ReadSettings_FromEmptyFile_Fails()
	{
		using IDisposable l = LogToConsole(out Log log);

		// arrange
		string tempFile = Path.GetTempFileName();
		var service = new SettingsService(log, tempFile);
		var vm = new MockSettings();

		// act
		Assert.ThrowsAsync<XmlException>(async () => { await service.Install(vm, () => "View", true); });
	}

	[Test]
	public async Task WriteSettings_ToEmptyFile()
	{
		using IDisposable l = LogToConsole(out Log log);

		// arrange
		string tempFile = Path.GetTempFileName();
		var service = new SettingsService(log, tempFile);
		var vm = new MockSettings { Value = 567 };

		// act
		_ = service.Install(vm, () => "View", false);
		await service.WriteCommand.Execute();

		// assert
		string text = await File.ReadAllTextAsync(tempFile);
		Assert.IsTrue(text.Contains("<int>567</int>"));
		Assert.AreEqual(0, log.DataStore.Entries.Count);
	}

	[Test]
	public async Task ReadSettings()
	{
		using IDisposable l = LogToConsole(out Log log);

		// arrange
		string tempFile = Path.GetTempFileName();
		await File.WriteAllTextAsync(tempFile, """
		                                       <?xml version="1.0" encoding="utf-8" standalone="yes"?>
		                                       <PalladiumSettings>
		                                         <ActionSettingsService>
		                                           <ActionSettings Guid="1a2ad44e-8623-4799-8545-c8ead5b6fecd" Type="System.Int32">
		                                             <int>567</int>
		                                           </ActionSettings>
		                                         </ActionSettingsService>
		                                       </PalladiumSettings>
		                                       """);

		var service = new SettingsService(log, tempFile);
		var vm = new MockSettings();

		// act
		await service.Install(vm, () => "View", true);

		// assert
		Assert.IsNotNull(vm.Observable);
		var task = vm.Observable!.FirstAsync().ToTask();
		Assert.IsTrue(task.IsCompleted);
		Assert.AreEqual(567, task.Result);
		Assert.AreEqual(0, log.DataStore.Entries.Count);
	}

	[Test]
	public void Install_MakesViewAvailable()
	{
		using IDisposable l = LogToConsole(out Log? log);

		// arrange
		string tempFile = Path.GetTempFileName();
		var service = new SettingsService(log, tempFile);
		var vm = new MockSettings();

		// act
		_ = service.Install(vm, () => "View", false);

		// assert
		Assert.AreEqual(1, service.SettingsViews.Count);
		Assert.AreEqual("View", service.SettingsViews.Items.First().CreateView.Invoke());
		Assert.AreEqual(0, log.DataStore.Entries.Count);
	}

	private static IDisposable LogToConsole(out Log log)
	{
		log = new Log();
		return log.DataStore.Entries
			.ToObservableChangeSet()
			.WhereReasonsAre(ListChangeReason.Add)
			.SelectMany(change => change.Select(i => i.Item.Current))
			.Where(changeSet => changeSet.Exception is not null)
			.Subscribe(Observer.Create<LogModel>(e => Console.WriteLine(e.Exception)));
	}

	private class MockSettings : ISettings<int>
	{
		public IObservable<int>? Observable;
		public int Value;

		/// <inheritdoc />
		public Guid SettingsGuid => new ("1A2AD44E-8623-4799-8545-C8EAD5B6FECD");

		/// <inheritdoc />
		public SettingsText SettingsText => new();

		/// <inheritdoc />
		public void ProcessDataObservable(IObservable<int> observable)
		{
			Observable = observable;
		}

		/// <inheritdoc />
		public int GetDataToSerialize()
		{
			return Value;
		}
	}
}