namespace Palladium.Builtin.Settings;

public interface IShortcutHandler
{
	/// <returns>The shortcut if it exists, null otherwise.</returns>
	Task<Shortcut?> TryGetStartupShortcut();

	Task CreateStartupShortcut(Shortcut shortcut);
	Task RemoveStartupShortcut();
}