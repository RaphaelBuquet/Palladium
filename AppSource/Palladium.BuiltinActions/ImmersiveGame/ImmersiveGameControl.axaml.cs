﻿using Avalonia.ReactiveUI;
using Palladium.ObservableExtensions.Lifecycle;

namespace Palladium.BuiltinActions.ImmersiveGame;

public partial class ImmersiveGameControl : ReactiveUserControl<ImmersiveGameViewModel>, IDisposable
{
	public ImmersiveGameControl()
	{
		InitializeComponent();
		this.InstallLifecycleHandler();
	}

	/// <inheritdoc />
	public void Dispose()
	{
		DataContext = null;
	}
}