using Avalonia.Controls;

namespace Palladium.AzureDevOps;

public class RoadmapGridViewModel
{
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

	public required List<GridLength> Columns { get; init; }

	public required List<GridLength> Rows { get; init; }

	public required List<IterationViewModel> IterationViewModels { get; init; }

	public required List<WorkItemViewModel> WorkItemViewModels { get; init; }
}