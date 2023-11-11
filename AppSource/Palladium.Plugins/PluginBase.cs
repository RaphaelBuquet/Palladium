using Palladium.ActionsService;
using Palladium.Logging;
using Palladium.Settings;

namespace Palladium.Plugins;

/// <summary>
///     Create a class that inherits from this. Palladium will set the fields and then call <see cref="Init" />.
/// </summary>
/// <remarks>
///     Fields are used instead of function parameters to simplify porting your plugins to newer versions of Palladium.
/// </remarks>
public abstract class PluginBase
{
	public ActionsRepositoryService? ActionsRepositoryService;
	public Log? ApplicationLog;
	public SettingsService? SettingsService;

	public void InstallDependencies(ActionsRepositoryService? actionsRepositoryService, Log? log, SettingsService? settingsService)
	{
		ActionsRepositoryService = actionsRepositoryService;
		ApplicationLog = log;
		SettingsService = settingsService;
	}
	
	/// <summary>
	///     Called by Palladium after the extension's DLL has been loaded. This is called from a background thread.
	/// </summary>
	public abstract void Init();
}