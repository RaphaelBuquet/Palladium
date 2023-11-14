using AzureDevOpsTools;

namespace Palladium.AzureDevOps;

public class WorkItemViewModel
{
	public WorkItemViewModel(RoadmapWorkItem workItem)
	{
		WorkItem = workItem;
	}
	
	public RoadmapWorkItem WorkItem { get; }

	public int StartColumnIndex { get; init; }
	public int RowIndex { get; init; }
	public int EndColumnIndexExclusive { get; init; }
	public int ColumnSpan => EndColumnIndexExclusive - StartColumnIndex;

	public string Title => WorkItem.Title;
}