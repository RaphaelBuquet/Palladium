using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.Work.WebApi;

namespace AzureDevOpsTools;

public struct RoadmapDefinition
{
	public string ProjectId;
	public string RoadmapId;

	/// <summary>
	///     Plan settings such as which teams are displayed on the roadmap or markers to display on the roadmap.
	/// </summary>
	public JsonTypes.PlanSettings PlanSettings;

	/// <summary>
	///     The iterations assigned to the teams to display in the roadmap.
	/// </summary>
	public List<TeamSettingsIteration> Iterations;

	/// <summary>
	///     The teams displayed in the roadmap.
	/// </summary>
	public List<WebApiTeam> Teams;

	/// <summary>
	///     Maps teams to their areas.
	/// </summary>
	public Dictionary<string, List<TeamFieldValue>> AreasByTeam;
}