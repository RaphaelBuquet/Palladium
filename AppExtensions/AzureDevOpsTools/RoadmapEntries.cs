namespace AzureDevOpsTools;

public struct RoadmapEntries
{
	public List<Column> Columns;
	public List<RoadmapWorkItem> RoadmapWorkItems;
}

public class Column
{
	public required string Iteration { get; init; }
	public required string DisplayName { get; init; }
	public DateTime StartDate { get; init; }
	public DateTime EndDate { get; init; }
	public double RelativeStart { get; init; }
	public double RelativeEnd { get; init; }
}

public class RoadmapWorkItem
{
	public required string Type { get; init; }
	public required string Title { get; init; }
	public required string State { get; init; }
	public string? AssignedTo { get; init; }
	public required Column Column { get; init; }
}