using Avalonia;
using Avalonia.Controls;

namespace Palladium.Views;

// note: using Window instead of ReactiveWindow for performance reasons. ReactiveWindow functionality is not needed here.
public partial class MainWindow : Window
{
	public MainWindow()
	{
		InitializeComponent();
		// using event pattern instead of Reactive pattern for performance reasons
		PropertyChanged += OnPropertyChanged;
	}

	private void OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
	{
		if (e.Property == WindowStateProperty)
		{
			if (WindowState == WindowState.Minimized)
			{
				Hide();
			}
		}
	}
}