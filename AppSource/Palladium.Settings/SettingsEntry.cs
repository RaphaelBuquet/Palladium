namespace Palladium.Settings;

public struct SettingsEntry
{
	public Guid Guid;
	public SettingsText Text;
	public Func<object> CreateView;
}