using Palladium.Logging;

namespace Palladium.BuiltinActions.ImmersiveGame;

public interface IDisplaySource
{
	MiniLog DisableNonPrimaryDisplays();
	MiniLog RestoreSettings();
	Task<string[]> GetDisplayDevices();
}