using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.Work.WebApi;

namespace AzureDevOpsTools;

public struct RoadmapDefinition
{
	public required string ProjectId;

	/// <summary>
	///     Plan settings such as which teams are displayed on the roadmap or markers to display on the roadmap.
	/// </summary>
	public required JsonTypes.PlanSettings PlanSettings;

	/// <summary>
	///     The iterations assigned to the teams to display in the roadmap.
	/// </summary>
	public required List<TeamSettingsIteration> Iterations;

	/// <summary>
	///     The teams displayed in the roadmap.
	/// </summary>
	public required List<WebApiTeam> Teams;

	/// <summary>
	///     Maps teams to their areas.
	/// </summary>
	public required Dictionary<string, List<TeamFieldValue>> AreasByTeam;
}