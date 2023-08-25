using System.Reactive.Linq;
using NSubstitute;
using Palladium.BuiltinActions.ImmersiveGame;

namespace Palladium.BuiltinActions.Tests;

public class ImmersiveGameViewModelTests
{
	[Test]
	public async Task TestBehaviour()
	{
		// arrange
		var s = Substitute.For<IDisplaySource>();
		var vm = new ImmersiveGameViewModel(s);

		// assert initial value
		Assert.IsFalse(vm.IsWorking);

		// refresh displays
		{
			// arrange
			var tcs = new TaskCompletionSource<string[]>();
			s.GetDisplayDevices().Returns(tcs.Task);

			// act
			vm.RefreshAvailableDisplays();

			// assert
			Assert.IsTrue(vm.IsWorking);

			// act
			tcs.SetResult(new [] { "hello" });

			// assert
			Assert.IsFalse(vm.IsWorking);
			Assert.IsTrue(vm.AvailableDisplays.Contains("hello"), $"\"{vm.AvailableDisplays}\" does not contain \"hello\"");
		}

		// commands
		{
			// act
			await vm.ActivateCommand.Execute();

			// assert
			s.Received(1).DisableNonPrimaryDisplays();
		}

		// commands
		{
			// act
			await vm.DeactivateCommand.Execute();

			// assert
			s.Received(1).RestoreSettings();
		}
	}
}