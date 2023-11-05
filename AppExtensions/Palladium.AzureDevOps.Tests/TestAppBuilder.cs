﻿using Avalonia;
using Avalonia.Headless;
using Avalonia.ReactiveUI;
using Palladium.AzureDevOps.Tests;
using Palladium.Headless;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace Palladium.AzureDevOps.Tests;

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