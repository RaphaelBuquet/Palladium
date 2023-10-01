namespace Palladium.Builtin.Settings;

public interface IShortcutHandler
{
	Task<bool> DoesStartupShortcutExist();
	Task CreateStartupShortcut();
	Task RemoveStartupShortcut();
}