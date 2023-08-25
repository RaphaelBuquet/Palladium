namespace Palladium.BuiltinActions.ImmersiveGame;

public interface IDisplaySource
{
	bool DisableNonPrimaryDisplays();
	bool RestoreSettings();
	Task<string[]> GetDisplayDevices();
}