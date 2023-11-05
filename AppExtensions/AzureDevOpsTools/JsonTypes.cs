namespace AzureDevOpsTools;

public static class JsonTypes
{
	public record PlanSettings(List<TeamBacklogMapping> TeamBacklogMappings);

	public record TeamBacklogMapping(string TeamId);
}