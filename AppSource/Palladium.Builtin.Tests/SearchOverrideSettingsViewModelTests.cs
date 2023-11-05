using System.Reactive;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Palladium.Builtin.SearchOverride;
using Palladium.Logging;
using Palladium.Settings;
using ReactiveUI;
using TestUtilities;

namespace Palladium.Builtin.Tests;

public class SearchOverrideSettingsViewModelTests
{
	[Test]
	public void UsesValuesFromSettings()
	{
		// arrange
		using IDisposable l = TestLog.LogToConsole(out Log log);
		var vm = new SearchOverrideSettingsViewModel(null, null);

		// act
		vm.Activator.Activate();
		vm.Data.OnNext(new SearchOverrideSettings
		{
			BrowserArguments = "hello",
			BrowserPath = "myapp",
			EnableOnAppStart = true
		});

		// assert
		Assert.AreEqual("hello", vm.BrowserArguments);
		Assert.AreEqual("myapp", vm.BrowserPath);
		Assert.AreEqual(true, vm.EnableOnAppStart);
		Assert.AreEqual(0, log.DataStore.Entries.Count);
	}

	[Test]
	public void WhenValueChanged_SettingsAreTriggered()
	{
		// arrange
		using IDisposable l = TestLog.LogToConsole(out Log log);
		var settingsService = Substitute.For<ISettingsService>();
		var mockCommand = ReactiveCommand.Create(() => { });
		settingsService.WriteCommand.Returns(mockCommand);
		var testScheduler = new TestScheduler();
		var observer = testScheduler.CreateObserver<Unit>();
		var vm = new SearchOverrideSettingsViewModel(null, settingsService, testScheduler);
		settingsService.WriteCommand.Subscribe(observer);

		// act
		vm.Activator.Activate();
		vm.BrowserArguments = "hello";
		testScheduler.AdvanceBy(1);

		vm.BrowserPath = "myapp";
		Thread.Sleep(100);
		testScheduler.AdvanceBy(1);

		vm.EnableOnAppStart = true;
		Thread.Sleep(100);
		testScheduler.AdvanceBy(1);

		// assert
		ReactiveAssert.AreElementsEqual(new []
		{
			ReactiveTest.OnNext(1, new Unit()),
			ReactiveTest.OnNext(2, new Unit()),
			ReactiveTest.OnNext(3, new Unit())
		}, observer.Messages);
		Assert.AreEqual(0, log.DataStore.Entries.Count);
	}
}