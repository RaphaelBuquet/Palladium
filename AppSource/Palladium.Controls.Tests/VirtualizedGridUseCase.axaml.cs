using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace Palladium.Controls.Tests;

public partial class VirtualizingGridUseCase : ReactiveUserControl<ItemsViewModel>
{
	public VirtualizingGridUseCase()
	{
		InitializeComponent();
		this.WhenActivated(disposables =>
		{
			this.WhenAnyValue(x => x.ViewModel!.RowDefinitions)
				.ObserveOn(RxApp.MainThreadScheduler)
				.Subscribe(rowDefinitions =>
				{
					if (VirtualizingGrid is null) return;
					VirtualizingGrid.RowDefinitions = rowDefinitions;
				})
				.DisposeWith(disposables);

			this.WhenAnyValue(x => x.ViewModel!.ColumnDefinitions)
				.ObserveOn(RxApp.MainThreadScheduler)
				.Subscribe(columnDefinitions =>
				{
					if (VirtualizingGrid is null) return;
					VirtualizingGrid.ColumnDefinitions = columnDefinitions;
				})
				.DisposeWith(disposables);
		});
	}

	private VirtualizingGrid? VirtualizingGrid => ItemsControl?.ItemsPanelRoot as VirtualizingGrid;
}

public class ItemsViewModel
{
	public List<ItemViewModel> Items { get; init; } = new ();

	public RowDefinitions RowDefinitions { get; init; } = new ();

	public ColumnDefinitions ColumnDefinitions { get; init; } = new ();
}

public class ItemViewModel
{
	public int RowIndex { get; init; } = 0;
	public int ColumnIndex { get; init; } = 0;
	public int RowSpan { get; init; } = 1;
	public int ColumnSpan { get; init; } = 1;
	public required int Height { get; init; }
	public required int Width { get; init; }
}