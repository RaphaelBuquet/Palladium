using Avalonia.Controls;

namespace Palladium.Views;

// note: using Window instead of ReactiveWindow for performance reasons. ReactiveWindow functionality is not needed here.
public partial class MainWindow : Window
{
	public MainWindow()
	{
		InitializeComponent();
	}
}