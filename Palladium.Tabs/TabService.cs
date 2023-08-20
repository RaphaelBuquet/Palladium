using Avalonia.Controls;

namespace Palladium.Tabs;

public class TabsService
{
	public TabControl? Target;
	
	public ContentControl AddNewTab(string header)
	{
		if (Target == null) throw new InvalidOperationException("Target has not been set.");

		var contentControl = new ContentControl();
		var newTab = new TabItem()
		{
            Content = contentControl,
            Header = header
		};
		Target.Items.Add(newTab);
		return contentControl;
	}
}