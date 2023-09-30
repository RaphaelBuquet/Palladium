using Palladium.Logging;

namespace Palladium.Builtin.ImmersiveGame;

public interface IDisplaySource
{
	MiniLog DisableNonPrimaryDisplays();
	MiniLog RestoreSettings();
	Task<string[]> GetDisplayDevices();
}