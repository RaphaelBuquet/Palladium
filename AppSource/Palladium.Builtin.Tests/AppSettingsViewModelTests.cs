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
	[Timeout(1000)]
	public async Task LaunchAtStartupIsChanging_ReflectsHandlerTaskStatus()
	{
		// arrange
		using IDisposable l = TestLog.LogToConsole(out Log log);
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

		{
			// act
			// the command processes the task result on another thread sometimes, so we need to watch for property changes.
			var launchAtStartup = vm.WhenAnyValue(x => x.LaunchAtStartup)
				.Skip(1)
				.FirstAsync()
				.Timeout(TimeSpan.FromMilliseconds(10))
				.ToTask();
			var launchAtStartupIsChanging = vm.WhenAnyValue(x => x.LaunchAtStartupIsChanging)
				.Skip(1)
				.FirstAsync()
				.Timeout(TimeSpan.FromMilliseconds(10))
				.ToTask();
			existsSource.SetResult(true);

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
			var launchAtStartupIsChanging = vm.WhenAnyValue(x => x.LaunchAtStartupIsChanging)
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
			var launchAtStartupIsChanging = vm.WhenAnyValue(x => x.LaunchAtStartupIsChanging)
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
			var launchAtStartupIsChanging = vm.WhenAnyValue(x => x.LaunchAtStartupIsChanging)
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
			var launchAtStartupIsChanging = vm.WhenAnyValue(x => x.LaunchAtStartupIsChanging)
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