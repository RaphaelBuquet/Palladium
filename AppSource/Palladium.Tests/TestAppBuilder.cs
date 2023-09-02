using Avalonia;
using Avalonia.Headless;
using Avalonia.ReactiveUI;
using Palladium.Headless;
using Palladium.Tests;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace Palladium.Tests;

public class TestAppBuilder
{
	public static AppBuilder BuildAvaloniaApp()
	{
		return AppBuilder.Configure<HeadlessApp>()
			.UseReactiveUI()
			.UseHeadless(new AvaloniaHeadlessPlatformOptions());
	}
}