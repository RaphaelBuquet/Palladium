using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;

namespace Palladium.Controls;

public class ApplicationTabItem : TabItem
{
	public static readonly StyledProperty<bool> AllowCloseProperty = AvaloniaProperty.Register<ApplicationTabItem, bool>(
		nameof(AllowClose), true);

	public static readonly StyledProperty<ICommand?> CloseTabCommandProperty = AvaloniaProperty.Register<ApplicationTabItem, ICommand?>(
		nameof(CloseTabCommand));

	public bool AllowClose
	{
		get => GetValue(AllowCloseProperty);
		set => SetValue(AllowCloseProperty, value);
	}

	public ICommand? CloseTabCommand
	{
		get => GetValue(CloseTabCommandProperty);
		set => SetValue(CloseTabCommandProperty, value);
	}
}