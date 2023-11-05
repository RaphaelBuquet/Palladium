using Avalonia;
using Avalonia.Headless;
using Avalonia.ReactiveUI;
using Palladium.Controls.Tests;
using Palladium.Headless;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace Palladium.Controls.Tests;

public class TestAppBuilder
{
	public static AppBuilder BuildAvaloniaApp()
	{
		return AppBuilder.Configure<HeadlessApp>()
			.UseReactiveUI()
			.WithInterFont()
			.UseHeadless(new AvaloniaHeadlessPlatformOptions());
	}
}