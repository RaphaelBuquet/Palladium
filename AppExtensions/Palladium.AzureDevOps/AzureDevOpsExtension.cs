using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using DynamicData;
using Palladium.ActionsService;
using Palladium.Plugins;

namespace Palladium.AzureDevOps;

// This class is instantiated at runtime by Palladium after the extension DLL is loaded.
[SuppressMessage("ReSharper", "UnusedType.Global")]
public class AzureDevOpsExtension : PluginBase
{
	private RoadmapSettingsViewModel? settingsVM;

	/// <inheritdoc />
	public override void Init()
	{
		// This is your plugin's entry point (this class inherits PluginBase).
		// Take a look at PluginBase to see what Palladium systems you have access to.
		// Keep this code fast to ensure plugins load quickly at startup.

		// Register actions. 
		ActionsRepositoryService?.Actions.AddOrUpdate(new ActionDescription(new Guid("783FB748-3DAF-4255-A13F-8D0BAE8305F4"))
			{
				Title = "Azure DevOps Roadmap",
				Description = "Display an ADO roadmap",
				Emoji = "🗓️",
				CanOpenMultiple = false,
				OnStart = StartAction
			}
		);

		settingsVM = new RoadmapSettingsViewModel(SettingsService);
		SettingsService?.Install(settingsVM, () => new RoadmapSettingsView() { DataContext = settingsVM }, true);
	}

	private void StartAction(ContentControl container)
	{
		var viewModel = new RoadmapViewModel(settingsVM, ApplicationLog);

		container.Content = new RoadmapView()
		{
			DataContext = viewModel
		};
	}
}