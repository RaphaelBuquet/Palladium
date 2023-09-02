using Palladium.BuiltinActions.ImmersiveGame;

namespace Palladium.BuiltinActions.Tests;

public class DisplayTests
{
	[Test]
	[Ignore("The test works but is annoying to run continuously")]
	public void EnableThenDisableTest()
	{
		if (WindowsDisplays.GetDisplayDevices().adapters
			    .Count(a => a.StateFlags.HasFlag(WindowsDisplays.DisplayDeviceStateFlags.DISPLAY_DEVICE_ACTIVE)) < 2)
		{
			Assert.Inconclusive("There are less than two active monitors on this system. The test cannot proceed.");
		}

		int disableResult = WindowsDisplays.DisableNonPrimaryDisplays();
		Assert.That(disableResult, Is.EqualTo(0));

		int restoreResult = WindowsDisplays.RestoreSettings();
		Assert.That(restoreResult, Is.EqualTo(0));
	}
}