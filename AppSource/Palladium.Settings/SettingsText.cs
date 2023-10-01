using Palladium.ActionsService;

namespace Palladium.Settings;

public struct SettingsText
{
	public string? Title;
	public string? SectionTitle;

	public static SettingsText FromActionDescription(ActionDescription? actionDescription)
	{
		return new SettingsText
		{
			Title = actionDescription?.Title,
			SectionTitle = $"{actionDescription?.Emoji} {actionDescription?.Title}"
		};
	}
}