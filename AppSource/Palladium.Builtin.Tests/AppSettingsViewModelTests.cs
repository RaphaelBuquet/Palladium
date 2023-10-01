using System.Reactive;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Palladium.Builtin.Settings;
using Palladium.Logging;
using ReactiveUI;
using TestUtilities;

namespace Palladium.Builtin.Tests;

public class AppSettingsViewModelTests
{
	[TestCase(false)]
	[TestCase(true)]
	public void LaunchAtStartup_InitialValue_IsFetchedFromHandler(bool initialValue)
	{
		// arrange
		using IDisposable l = TestLog.LogToConsole(out Log log);
		var handler = Substitute.For<IShortcutHandler>();
		handler.DoesStartupShortcutExist().Returns(Task.FromResult(initialValue));
		var vm = new AppSettingsViewModel(handler, log);

		// act
		((IActivatableViewModel)vm).Activator.Activate();

		// assert
		Assert.AreEqual(initialValue, vm.LaunchAtStartup);
	}

	[TestCase]
	public void ToggleOn_LaunchAtStartup_CallsHandler()
	{
		// arrange
		using IDisposable l = TestLog.LogToConsole(out Log log);
		var handler = Substitute.For<IShortcutHandler>();
		handler.DoesStartupShortcutExist().Returns(Task.FromResult(false));
		var vm = new AppSettingsViewModel(handler, log);

		// act
		((IActivatableViewModel)vm).Activator.Activate();
		vm.LaunchAtStartup = true;

		// assert
		handler.Received().CreateStartupShortcut();
	}

	[TestCase]
	public void ToggleOff_LaunchAtStartup_CallsHandler()
	{
		// arrange
		using IDisposable l = TestLog.LogToConsole(out Log log);
		var handler = Substitute.For<IShortcutHandler>();
		handler.DoesStartupShortcutExist().Returns(Task.FromResult(true));
		var vm = new AppSettingsViewModel(handler, log);

		// act
		((IActivatableViewModel)vm).Activator.Activate();
		vm.LaunchAtStartup = false;

		// assert
		handler.Received().RemoveStartupShortcut();
	}

	[TestCase]
	[Ignore("The tasks are watched on another thread and there isn't an easy wait to wait on the properties being changed.")]
	public void LaunchAtStartupIsChanging_ReflectsHandlerTaskStatus()
	{
		// arrange
		using IDisposable l = TestLog.LogToConsole(out Log log);
		var s = new TestScheduler();
		var handler = Substitute.For<IShortcutHandler>();
		var existsSource = new TaskCompletionSource<bool>();
		var createSource = new TaskCompletionSource<Unit>();
		var removeSource = new TaskCompletionSource<Unit>();
		handler.DoesStartupShortcutExist().Returns(existsSource.Task);
		handler.CreateStartupShortcut().Returns(createSource.Task);
		handler.RemoveStartupShortcut().Returns(removeSource.Task);
		var vm = new AppSettingsViewModel(handler, log);

		// act
		((IActivatableViewModel)vm).Activator.Activate();

		// assert
		Assert.AreEqual(true, vm.LaunchAtStartupIsChanging);
		Assert.AreEqual(false, vm.LaunchAtStartup);

		// act
		existsSource.SetResult(true);
		// Thread.Sleep(10);

		// assert
		Assert.AreEqual(false, vm.LaunchAtStartupIsChanging);
		Assert.AreEqual(true, vm.LaunchAtStartup);

		// act
		vm.LaunchAtStartup = false;

		// assert
		Assert.AreEqual(true, vm.LaunchAtStartupIsChanging);
		Assert.AreEqual(false, vm.LaunchAtStartup);

		// act
		removeSource.SetResult(new Unit());
		Thread.Sleep(10);

		// assert
		Assert.AreEqual(false, vm.LaunchAtStartupIsChanging);
		Assert.AreEqual(false, vm.LaunchAtStartup);

		// act
		vm.LaunchAtStartup = true;

		// assert
		Assert.AreEqual(true, vm.LaunchAtStartupIsChanging);
		Assert.AreEqual(true, vm.LaunchAtStartup);

		// act
		createSource.SetResult(new Unit());
		Thread.Sleep(10);

		// assert
		Assert.AreEqual(false, vm.LaunchAtStartupIsChanging);
		Assert.AreEqual(true, vm.LaunchAtStartup);
	}
}