using Avalonia.Controls;
using DynamicData;
using Palladium.ActionsService;
using Palladium.Extensions;

namespace PalladiumExtensionTemplate;

// This class is instantiated at runtime by Palladium after the extension DLL is loaded.
public class MyExtension : ExtensionBase
{
	/// <inheritdoc />
	public override void Init()
	{
		// This is your extension's entry point (this class inherits ExtensionBase).
		// Take a look at ExtensionBase to see what Palladium systems you have access to.
		// Keep this code fast to ensure extensions load quickly at startup.

		// Register actions. 
		// Make sure you use a unique GUID.
		ActionsRepositoryService?.Actions.AddOrUpdate(new ActionDescription(new Guid("00000000-FFFF-EEEE-DDDD-000000000000"))
			{
				Title = "Example Extension Action",
				Description = "Example extension description",
				Emoji = "⚠️",
				CanOpenMultiple = false,
				OnStart = StartExampleAction
			}
		);
	}

	private void StartExampleAction(ContentControl container)
	{
		// Build and set your view model here
		var viewModel = new object();

		// Replace the TextBlock with your custom control, to build the view.
		container.Content = new TextBlock
		{
			DataContext = viewModel,
			Text = "Hello World!"
		};
	}
}