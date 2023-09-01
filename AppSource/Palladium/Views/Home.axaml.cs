using System;
using Avalonia.ReactiveUI;
using Palladium.ObservableExtensions.Lifecycle;
using Palladium.ViewModels;

namespace Palladium.Views;

public partial class Home : ReactiveUserControl<HomeViewModel>, IDisposable
{
	public Home()
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