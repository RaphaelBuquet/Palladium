using Avalonia;
using Avalonia.Controls;

namespace Palladium.AzureDevOps;

public class RoadmapGridViewModel
{
	public required List<GridLength> Columns { get; init; }

	public required List<GridLength> Rows { get; init; }

	public required List<IterationViewModel> IterationViewModels { get; init; }

	public required List<WorkItemViewModel> WorkItemViewModels { get; init; }

	/// <summary>
	///     <para>
	///         When this is not null, this should be used to set the scrollbar position.
	///     </para>
	/// </summary>
	/// <remarks>
	///     <para>
	///         For the X offset, we want the normalised position to represent the middle of the screen,
	///         such that for a value of 0.5 the viewport will be centered,
	///         rather than being offset a bit to the right.
	///     </para>
	///     <para>
	///         The value should only be provided the first time this view model is emitted. When this view model is emitted
	///         again,
	///         it should represent the user clicking "refresh" and we don't want to move the scrollbar when the user clicks
	///         refresh as the user could have manually moved the scrollbar.
	///     </para>
	///     <para>
	///         This is provided as part of the <see cref="RoadmapGridViewModel" /> instead of being a separate observable on
	///         <see cref="RoadmapViewModel" />, because the view code that handles the grid also needs to handle the scroll
	///         bar. It's best to emit all the data at once, instead of over two separate observables, because the order in
	///         which two notifications are processed is not deterministic.
	///     </para>
	/// </remarks>
	public Vector? InitialScrollbarNormalisedPosition { get; init; }

	public static RoadmapGridViewModel Empty()
	{
		return new RoadmapGridViewModel()
		{
			IterationViewModels = new List<IterationViewModel>(),
			WorkItemViewModels = new List<WorkItemViewModel>(),
			Columns = new List<GridLength>(),
			Rows = new List<GridLength>()
		};
	}
}