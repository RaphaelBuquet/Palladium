﻿using Avalonia;
using Avalonia.Markup.Xaml;

namespace Palladium.Tests;

public class HeadlessApp : Application
{
	public override void Initialize()
	{
		AvaloniaXamlLoader.Load(this);
	}
}