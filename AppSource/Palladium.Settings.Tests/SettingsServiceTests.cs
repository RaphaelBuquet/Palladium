using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Palladium.Logging;
using TestUtilities;

namespace Palladium.Settings.Tests;

public class SettingsServiceTests
{
	[Test]
	public async Task ReadSettings_FromEmptyFile_Fails()
	{
		using IDisposable l = TestLog.LogToConsole(out Log log);

		// arrange
		string tempFile = Path.GetTempFileName();
		var service = new SettingsService(log, tempFile);
		var vm = new MockSettings("1A2AD44E-8623-4799-8545-C8EAD5B6FECD");

		// act
		await service.Install(vm, () => "View", true);
		// assert
		Assert.AreEqual(1, log.DataStore.Entries.Count);
	}

	[Test]
	public async Task WriteSettings_ToEmptyFile()
	{
		using IDisposable l = TestLog.LogToConsole(out Log log);

		// arrange
		string tempFile = Path.GetTempFileName();
		var service = new SettingsService(log, tempFile);
		var vm = new MockSettings("1A2AD44E-8623-4799-8545-C8EAD5B6FECD") { Value = 567 };

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
		using IDisposable l = TestLog.LogToConsole(out Log log);

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
		var vm = new MockSettings("1A2AD44E-8623-4799-8545-C8EAD5B6FECD");

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
	public async Task UpdatingExistingSettings_DoesNotCreateAnotherSettingsEntry()
	{
		using IDisposable l = TestLog.LogToConsole(out Log log);

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
		var vm = new MockSettings("1A2AD44E-8623-4799-8545-C8EAD5B6FECD");
		await service.Install(vm, () => "View", true);

		// act
		vm.Value = 10;
		await service.WriteCommand.Execute();

		// assert
		string fileContents = await File.ReadAllTextAsync(tempFile);
		var expectedResult = """
		                     <?xml version="1.0" encoding="utf-8" standalone="yes"?>
		                     <PalladiumSettings>
		                       <ActionSettingsService>
		                         <ActionSettings Guid="1a2ad44e-8623-4799-8545-c8ead5b6fecd" Type="System.Int32">
		                           <int>10</int>
		                         </ActionSettings>
		                       </ActionSettingsService>
		                     </PalladiumSettings>
		                     """;
		Assert.AreEqual(expectedResult, fileContents);
		Assert.AreEqual(0, log.DataStore.Entries.Count);
	}

	[TestCase(
		"""
		<?xml version="1.0" encoding="utf-8" standalone="yes"?>
		<PalladiumSettings>
		  <ActionSettingsService>
		    <ActionSettings Guid="d5ac6569-d165-4fbb-b688-c4348d59841f" Type="System.Int32">
		      <int>0</int>
		    </ActionSettings>
		    <ActionSettings Guid="1a2ad44e-8623-4799-8545-c8ead5b6fecd" Type="System.Int32">
		      <int>567</int>
		    </ActionSettings>
		  </ActionSettingsService>
		</PalladiumSettings>
		""",
		"""
		<?xml version="1.0" encoding="utf-8" standalone="yes"?>
		<PalladiumSettings>
		  <ActionSettingsService>
		    <ActionSettings Guid="1a2ad44e-8623-4799-8545-c8ead5b6fecd" Type="System.Int32">
		      <int>10</int>
		    </ActionSettings>
		    <ActionSettings Guid="d5ac6569-d165-4fbb-b688-c4348d59841f" Type="System.Int32">
		      <int>0</int>
		    </ActionSettings>
		  </ActionSettingsService>
		</PalladiumSettings>
		""", TestName = "TwoSettings"
	)]
	public async Task UpdatingExistingSettings_WithMultipleSettings_DoesNotCreateAnotherSettingsEntry(string existingSettings, string expectedResult)
	{
		using IDisposable l = TestLog.LogToConsole(out Log log);

		// arrange
		string tempFile = Path.GetTempFileName();
		await File.WriteAllTextAsync(tempFile, existingSettings);
		var service = new SettingsService(log, tempFile);
		var vm1 = new MockSettings("1A2AD44E-8623-4799-8545-C8EAD5B6FECD");
		var vm2 = new MockSettings("d5ac6569-d165-4fbb-b688-c4348d59841f");
		await service.Install(vm1, () => "View1", true);
		await service.Install(vm2, () => "View2", true);

		// act
		vm1.Value = 10;
		await service.WriteCommand.Execute();

		// assert
		string fileContents = await File.ReadAllTextAsync(tempFile);
		Assert.AreEqual(expectedResult, fileContents);
		Assert.AreEqual(0, log.DataStore.Entries.Count);
	}

	[Test]
	public async Task Install_MakesViewAvailable()
	{
		using IDisposable l = TestLog.LogToConsole(out Log? log);

		// arrange
		string tempFile = Path.GetTempFileName();
		var service = new SettingsService(log, tempFile);
		var vm = new MockSettings("1A2AD44E-8623-4799-8545-C8EAD5B6FECD");

		// act
		await service.Install(vm, () => "View", false);

		// assert
		Assert.AreEqual(1, service.SettingsViews.Count);
		Assert.AreEqual("View", service.SettingsViews.Items.First().CreateView.Invoke());
		Assert.AreEqual(0, log.DataStore.Entries.Count);
	}

	[Test]
	public async Task InstallingMultipleSettingsAtTheSameTime()
	{
		using IDisposable l = TestLog.LogToConsole(out Log log);

		// arrange
		string tempFile = Path.GetTempFileName();
		await File.WriteAllTextAsync(tempFile, """
		                                       <?xml version="1.0" encoding="utf-8" standalone="yes"?>
		                                       <PalladiumSettings>
		                                       </PalladiumSettings>
		                                       """);
		var service = new SettingsService(log, tempFile);
		var vm1 = new MockSettings("1A2AD44E-8623-4799-8545-C8EAD5B6FECD");
		var vm2 = new MockSettings("d5ac6569-d165-4fbb-b688-c4348d59841f");

		// act
		Task t1 = service.Install(vm1, () => "View1", true);
		Task t2 = service.Install(vm2, () => "View2", true);
		await Task.WhenAll(t1, t2);

		// assert
		Assert.AreEqual(0, log.DataStore.Entries.Count);
	}

	private class MockSettings : ISettings<int>
	{
		public IObservable<int>? Observable;
		public int Value;

		public MockSettings(string settingsGuid)
		{
			SettingsGuid = Guid.Parse(settingsGuid);
		}

		/// <inheritdoc />
		public Guid SettingsGuid { get; }

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