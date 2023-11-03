using Avalonia.Controls;
using DynamicData;
using Palladium.ActionsService;
using Palladium.Extensions;

namespace PalladiumExtensionTemplate;

// This class is instantiated at runtime by Palladium after the extension DLL is loaded.
// TODO: Rename this.
public class MyExtension : ExtensionBase
{
	/// <inheritdoc />
	public override void Init()
	{
		// This is your extension's entry point (this class inherits ExtensionBase).
		// Take a look at ExtensionBase to see what Palladium systems you have access to.
		// Keep this code fast to ensure extensions load quickly at startup.

		// Register actions. 
		// TODO: Generate a GUID.
		ActionsRepositoryService?.Actions.AddOrUpdate(new ActionDescription(new Guid("00000000-FFFF-EEEE-DDDD-000000000000"))
			{
				Title = "Example Extension Action", // TODO
				Description = "Example extension description", // TODO
				Emoji = "⚠️", // TODO
				CanOpenMultiple = false, // TODO
				OnStart = StartAction
			}
		);
	}

	private void StartAction(ContentControl container)
	{
		// TODO: Build and set your view model here (replace with your view model type).
		var viewModel = new object();

		// TODO: Replace the TextBlock with your custom control, to build the view.
		container.Content = new TextBlock
		{
			DataContext = viewModel,
			Text = "Hello World!" // TODO: Remove this.
		};
	}
}