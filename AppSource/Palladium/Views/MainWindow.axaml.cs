using Avalonia.Controls;
using Avalonia.ReactiveUI;
using Palladium.ViewModels;

namespace Palladium.Views;

public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
	public MainWindow()
	{
		InitializeComponent();
	}
}