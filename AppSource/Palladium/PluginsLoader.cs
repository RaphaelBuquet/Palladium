using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Palladium.ActionsService;
using Palladium.AzureDevOps;
using Palladium.Logging;
using Palladium.Plugins;
using Palladium.Settings;

namespace Palladium;

public static class PluginsLoader
{
	public static List<PluginBase> Plugins => new ()
	{
		new AzureDevOpsExtension()
	};
	
	public static void LoadAll(ActionsRepositoryService actionsRepositoryService, Log? log, SettingsService settingsService)
	{
		foreach (var plugin in Plugins)
		{
			try
			{
				plugin.InstallDependencies(actionsRepositoryService, log, settingsService);
				plugin.Init();
				log?.Emit(new EventId(), LogLevel.Information, $"Loaded plugin \"{plugin.GetType().FullName}\".");
			}
			catch (Exception e)
			{
				log?.Emit(new EventId(), LogLevel.Error, $"Failed to initialize \"{plugin.GetType().FullName}\".", e);
			}
		}
	}
}