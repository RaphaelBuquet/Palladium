﻿using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using DynamicData;
using Palladium.ActionsService;
using Palladium.Extensions;

namespace Palladium.AzureDevOps;

// This class is instantiated at runtime by Palladium after the extension DLL is loaded.
[SuppressMessage("ReSharper", "UnusedType.Global")]
public class AzureDevOpsExtension : ExtensionBase
{
	/// <inheritdoc />
	public override void Init()
	{
		// This is your extension's entry point (this class inherits ExtensionBase).
		// Take a look at ExtensionBase to see what Palladium systems you have access to.
		// Keep this code fast to ensure extensions load quickly at startup.

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

		// SettingsService.Install() TODO
	}

	private void StartAction(ContentControl container)
	{
		var viewModel = new RoadmapViewModel();

		container.Content = new RoadmapView()
		{
			DataContext = viewModel
		};
	}
}