using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
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
		handler.TryGetStartupShortcut().Returns(Task.FromResult<Shortcut?>(initialValue ? new Shortcut() : null));
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
		handler.TryGetStartupShortcut().Returns(Task.FromResult<Shortcut?>(null));
		var vm = new AppSettingsViewModel(handler, log);

		// act
		((IActivatableViewModel)vm).Activator.Activate();
		vm.LaunchAtStartup = true;

		// assert
		handler.Received().CreateStartupShortcut(Arg.Any<Shortcut>());
	}

	[TestCase]
	public void ToggleOff_LaunchAtStartup_CallsHandler()
	{
		// arrange
		using IDisposable l = TestLog.LogToConsole(out Log log);
		var handler = Substitute.For<IShortcutHandler>();
		handler.TryGetStartupShortcut().Returns(Task.FromResult<Shortcut?>(new Shortcut()));
		var vm = new AppSettingsViewModel(handler, log);

		// act
		((IActivatableViewModel)vm).Activator.Activate();
		vm.LaunchAtStartup = false;

		// assert
		handler.Received().RemoveStartupShortcut();
	}

	[TestCase]
	public void ToggleOn_LaunchMinimised_UpdatesExistingShortcut()
	{
		// arrange
		using IDisposable l = TestLog.LogToConsole(out Log log);
		var handler = Substitute.For<IShortcutHandler>();
		handler.TryGetStartupShortcut().Returns(Task.FromResult<Shortcut?>(new Shortcut()));
		var vm = new AppSettingsViewModel(handler, log);

		// act
		((IActivatableViewModel)vm).Activator.Activate();
		vm.StartMinimised = true;

		// assert argument was added
		handler.Received().CreateStartupShortcut(Arg.Is<Shortcut>(arg => arg == new Shortcut { Arguments = "--minimised" }));
	}

	[TestCase]
	public void ToggleOff_LaunchMinimised_UpdatesExistingShortcut()
	{
		// arrange
		using IDisposable l = TestLog.LogToConsole(out Log log);
		var handler = Substitute.For<IShortcutHandler>();
		handler.TryGetStartupShortcut().Returns(Task.FromResult<Shortcut?>(new Shortcut { Arguments = "--minimised" }));
		var vm = new AppSettingsViewModel(handler, log);

		// act
		((IActivatableViewModel)vm).Activator.Activate();
		vm.StartMinimised = false;

		// assert argument was removed
		handler.Received().CreateStartupShortcut(Arg.Is<Shortcut>(arg => arg == new Shortcut()));
	}

	[TestCase]
	public void LaunchMinimised_AlwaysOff_WhenLaunchAtStartup_IsOff()
	{
		// arrange
		using IDisposable l = TestLog.LogToConsole(out Log log);
		var handler = Substitute.For<IShortcutHandler>();
		handler.TryGetStartupShortcut().Returns(Task.FromResult<Shortcut?>(null));
		var vm = new AppSettingsViewModel(handler, log);

		// act
		((IActivatableViewModel)vm).Activator.Activate();
		vm.StartMinimised = true;

		// assert
		Assert.AreEqual(false, vm.StartMinimised);
	}

	[TestCase]
	public void ToggleOff_LaunchAtStartup_TogglesOff_LaunchMinimised()
	{
		// arrange
		using IDisposable l = TestLog.LogToConsole(out Log log);
		var handler = Substitute.For<IShortcutHandler>();
		handler.TryGetStartupShortcut().Returns(Task.FromResult<Shortcut?>(new Shortcut { Arguments = "--minimised" }));
		var vm = new AppSettingsViewModel(handler, log);

		// act
		((IActivatableViewModel)vm).Activator.Activate();
		vm.LaunchAtStartup = false;

		// assert
		Assert.AreEqual(false, vm.StartMinimised);
	}

	[TestCase]
	[Timeout(1000)]
	public async Task LaunchAtStartupIsChanging_ReflectsHandlerTaskStatus()
	{
		// arrange
		using IDisposable l = TestLog.LogToConsole(out Log log);
		var handler = Substitute.For<IShortcutHandler>();
		var existsSource = new TaskCompletionSource<Shortcut?>();
		var createSource = new TaskCompletionSource<Unit>();
		var removeSource = new TaskCompletionSource<Unit>();
		handler.TryGetStartupShortcut().Returns(existsSource.Task);
		handler.CreateStartupShortcut(Arg.Any<Shortcut>()).Returns(createSource.Task);
		handler.RemoveStartupShortcut().Returns(removeSource.Task);
		var vm = new AppSettingsViewModel(handler, log);

		// act
		((IActivatableViewModel)vm).Activator.Activate();

		// assert
		Assert.AreEqual(true, vm.ShortcutIsChanging);
		Assert.AreEqual(false, vm.LaunchAtStartup);

		{
			// act
			// the command processes the task result on another thread sometimes, so we need to watch for property changes.
			var launchAtStartup = vm.WhenAnyValue(x => x.LaunchAtStartup)
				.Skip(1)
				.FirstAsync()
				.Timeout(TimeSpan.FromMilliseconds(10))
				.ToTask();
			var launchAtStartupIsChanging = vm.WhenAnyValue(x => x.ShortcutIsChanging)
				.Skip(1)
				.FirstAsync()
				.Timeout(TimeSpan.FromMilliseconds(10))
				.ToTask();
			existsSource.SetResult(new Shortcut());

			// assert
			Assert.AreEqual(false, await launchAtStartupIsChanging);
			Assert.AreEqual(true, await launchAtStartup);
		}

		{
			// act
			var launchAtStartup = vm.WhenAnyValue(x => x.LaunchAtStartup)
				.Skip(1)
				.FirstAsync()
				.Timeout(TimeSpan.FromMilliseconds(10))
				.ToTask();
			var launchAtStartupIsChanging = vm.WhenAnyValue(x => x.ShortcutIsChanging)
				.Skip(1)
				.FirstAsync()
				.Timeout(TimeSpan.FromMilliseconds(10))
				.ToTask();
			vm.LaunchAtStartup = false;

			// assert
			Assert.AreEqual(true, await launchAtStartupIsChanging);
			Assert.AreEqual(false, await launchAtStartup);
		}

		{
			// act
			var launchAtStartupIsChanging = vm.WhenAnyValue(x => x.ShortcutIsChanging)
				.Skip(1)
				.FirstAsync()
				.Timeout(TimeSpan.FromMilliseconds(10))
				.ToTask();
			removeSource.SetResult(new Unit());

			// assert
			Assert.AreEqual(false, await launchAtStartupIsChanging);
			Assert.AreEqual(false, vm.LaunchAtStartup); // it's expected to stay the same so don't wait for a change
		}

		{
			// act
			var launchAtStartup = vm.WhenAnyValue(x => x.LaunchAtStartup)
				.Skip(1)
				.FirstAsync()
				.Timeout(TimeSpan.FromMilliseconds(10))
				.ToTask();
			var launchAtStartupIsChanging = vm.WhenAnyValue(x => x.ShortcutIsChanging)
				.Skip(1)
				.FirstAsync()
				.Timeout(TimeSpan.FromMilliseconds(10))
				.ToTask();
			vm.LaunchAtStartup = true;

			// assert
			Assert.AreEqual(true, await launchAtStartupIsChanging);
			Assert.AreEqual(true, await launchAtStartup);
		}

		{
			var launchAtStartupIsChanging = vm.WhenAnyValue(x => x.ShortcutIsChanging)
				.Skip(1)
				.FirstAsync()
				.Timeout(TimeSpan.FromMilliseconds(10))
				.ToTask();

			// act
			createSource.SetResult(new Unit());

			// assert
			Assert.AreEqual(false, await launchAtStartupIsChanging);
			Assert.AreEqual(true, vm.LaunchAtStartup); // it's expected to stay the same so don't wait for a change
		}
	}
}