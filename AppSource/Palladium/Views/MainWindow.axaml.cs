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

		// it's useful to have no min size in dev as it makes it easier to stress-test the app for overflow issues.
#if !DEBUG
		MinWidth = 600;
		MinHeight = 300;
#endif
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