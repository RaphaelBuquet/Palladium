using Avalonia;
using Avalonia.Headless;
using Palladium.Tests;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace Palladium.Tests;

public class TestAppBuilder
{
	public static AppBuilder BuildAvaloniaApp()
	{
		return AppBuilder.Configure<HeadlessApp>()
			.UseHeadless(new AvaloniaHeadlessPlatformOptions());
	}
}