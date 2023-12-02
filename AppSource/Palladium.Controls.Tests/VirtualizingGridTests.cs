using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;

namespace Palladium.Controls.Tests;

public class VirtualizingGridTests
{
	// [AvaloniaTest]
	// public void TEMP_REMOVE_ME()
	// {
	// 	var grid = new Grid()
	// 	{
	// 		// Children = { new TextBlock()}
	// 	};
	// 	var window = new Window()
	// 	{
	// 		Width = 100,
	// 		Height = 100,
	// 		Content = new WrapPanel()
	// 		{
	// 			Children =
	// 			{
	// 				grid
	// 			}
	// 		}
	// 	};
	// 	window.Show();
	// 	Dispatcher.UIThread.RunJobs();
	// 	Assert.AreEqual(0, grid.Bounds.Width);
	// 	Assert.AreEqual(0, grid.Bounds.Height);
	// }

	[AvaloniaTest]
	public void Empty()
	{
		var control = new VirtualizingGridUseCase();
		Window window = CreateWindow(control);
		window.Show();
		Dispatcher.UIThread.RunJobs();

		Assert.AreEqual(0, control.Bounds.Width);
		Assert.AreEqual(0, control.Bounds.Height);
	}

	[AvaloniaTest]
	public void OneItem()
	{
		var control = new VirtualizingGridUseCase()
		{
			DataContext = new ItemsViewModel()
			{
				Items = new List<ItemViewModel>() { new ()  { Width = 10, Height = 10 } }
			}
		};
		Window window = CreateWindow(control);
		window.Show();
		Dispatcher.UIThread.RunJobs();

		Assert.AreEqual(10, control.Bounds.Width);
		Assert.AreEqual(10, control.Bounds.Height);
	}

	[AvaloniaTest]
	public void MultipleItemsOnOneRow()
	{
		var control = new VirtualizingGridUseCase()
		{
			DataContext = new ItemsViewModel()
			{
				Items = new List<ItemViewModel>()
				{
					new ()
					{
						Width = 10, Height = 10, ColumnIndex = 0
					},
					new ()
					{
						Width = 10, Height = 10, ColumnIndex = 1
					}
				},
				ColumnDefinitions = new ColumnDefinitions()
				{
					new (GridLength.Auto),
					new (GridLength.Auto)
				},
				RowDefinitions = new RowDefinitions()
				{
					new (GridLength.Auto)
				}
			}
		};
		Window window = CreateWindow(control);
		window.Show();
		Dispatcher.UIThread.RunJobs();

		Assert.AreEqual(20, control.Bounds.Width);
		Assert.AreEqual(10, control.Bounds.Height);
	}

	private static Window CreateWindow(Control control)
	{
		return new Window()
		{
			Width = 100,
			Height = 100,
			Content = new WrapPanel()
			{
				Children =
				{
					control
				}
			}
		};
	}
}