namespace AzureDevOpsTools;

public struct RoadmapEntries
{
	public List<Iteration> Iterations;
	public List<RoadmapWorkItem> RoadmapWorkItems;
}

public class Iteration
{
	public required string IterationPath { get; init; }
	public required string DisplayName { get; init; }
	public required DateTime StartDate { get; init; }
	public required DateTime EndDate { get; init; }
}

public class RoadmapWorkItem
{
	public required int? Id { get; init; }
	public required string Type { get; init; }
	public required string Title { get; init; }
	public required string State { get; init; }
	public string? AssignedTo { get; init; }
	public required Iteration Iteration { get; init; }
}