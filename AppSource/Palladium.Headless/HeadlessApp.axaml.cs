using Avalonia;
using Avalonia.Markup.Xaml;

namespace Palladium.Headless;

public class HeadlessApp : Application
{
	public override void Initialize()
	{
		AvaloniaXamlLoader.Load(this);
	}
}