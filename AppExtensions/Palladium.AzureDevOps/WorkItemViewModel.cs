using System.Reactive;
using Avalonia.Media;
using AzureDevOpsTools;
using ReactiveUI;

namespace Palladium.AzureDevOps;

public class WorkItemViewModel
{
	public WorkItemViewModel(RoadmapWorkItem workItem)
	{
		WorkItem = workItem;
	}

	public RoadmapWorkItem WorkItem { get; }
	public WorkItemStyles? WorkItemStyles { get; set; }
	public int StartColumnIndex { get; init; }
	public int RowIndex { get; init; }
	public int EndColumnIndexExclusive { get; init; }
	public int ColumnSpan => EndColumnIndexExclusive - StartColumnIndex;
	public ReactiveCommand<RoadmapWorkItem, Unit>? OpenTicketCommand { get; set; }

	public Color TypeColour
	{
		get
		{
			if (WorkItemStyles?.TypeToColour.TryGetValue(WorkItem.Type, out Color color) != true)
			{
				color = Colors.Magenta;
			}
			return color;
		}
	}

	public Color StateColour
	{
		get
		{
			if (WorkItemStyles?.StateToColour.TryGetValue(new WorkItemState { WorkItemType = WorkItem.Type, State = WorkItem.State }, out Color color) != true)
			{
				color = Colors.Magenta;
			}
			return color;
		}
	}
}