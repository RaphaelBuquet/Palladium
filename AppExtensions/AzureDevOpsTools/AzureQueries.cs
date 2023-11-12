using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.Core.WebApi.Types;
using Microsoft.TeamFoundation.Work.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.Process.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.Process.WebApi.Models;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Newtonsoft.Json.Linq;

namespace AzureDevOpsTools;

public static class AzureQueries
{
	public static async Task<VssConnection> ConnectWithToken(string projectUrl, string token)
	{
		var credentials = new VssBasicCredential("", token);
		var connection = new VssConnection(new Uri(projectUrl), credentials);
		await connection.ConnectAsync();

		// try accessing projects to verify the token
		var projectHttpClient = await connection.GetClientAsync<ProjectHttpClient>();
		await projectHttpClient.GetProjects();

		return connection;
	}

	public static async Task<List<string>> GetAutomaticRoadmapTypes(VssConnection connection, string projectId)
	{
		var result = new List<string>();
		var workItemTypes = await GetWorkItemTypes(connection, projectId);

		const string userStory = "User Story";
		const string bug = "Bug";
		const string pbi = "Product Backlog Item";
		if (workItemTypes.Count(t => t.Name == userStory) == 1)
		{
			result.Add(userStory);
		}
		else if (workItemTypes.Count(t => t.Name == pbi) == 1)
		{
			result.Add(pbi);
		}

		if (workItemTypes.Count(t => t.Name == bug) == 1)
		{
			result.Add(bug);
		}

		return result;
	}

	public static async Task<List<WorkItemTypeModel>> GetWorkItemTypes(VssConnection connection, string projectId)
	{
		var workItemTrackingProcessClient = connection.GetClient<WorkItemTrackingProcessHttpClient>();
		string? processId = null;
		TeamProject? project = await GetProject(connection, projectId);
		Dictionary<string, string>? processTemplate = null;
		project?.Capabilities.TryGetValue("processTemplate", out processTemplate);
		processTemplate?.TryGetValue("templateTypeId", out processId);
		if (processId == null)
		{
			return new List<WorkItemTypeModel>();
		}
		return await workItemTrackingProcessClient.GetWorkItemTypesAsync(new Guid(processId));
	}

	public static Task<TeamProject?> GetProject(VssConnection connection, string projectId)
	{
		var workClient = connection.GetClient<ProjectHttpClient>();
		return workClient.GetProject(projectId, true);
	}

	public static Task<Plan?> GetPlan(VssConnection connection, string projectId, string planId)
	{
		var workClient = connection.GetClient<WorkHttpClient>();
		return workClient.GetPlanAsync(projectId, planId);
	}

	public static async Task<RoadmapDefinition> GetRoadmapDefinition(VssConnection connection, string projectId, string planId)
	{
		var workClient = connection.GetClient<WorkHttpClient>();
		var teamHttpClient = connection.GetClient<TeamHttpClient>();
		Plan? plan = await workClient.GetPlanAsync(projectId, planId);
		if (plan.Properties is not JObject jObject)
		{
			return new RoadmapDefinition();
		}

		var settings = jObject.ToObject<JsonTypes.PlanSettings>();
		if (settings == null)
		{
			return new RoadmapDefinition();
		}

		var teamsContexts = Enumerable.Select<JsonTypes.TeamBacklogMapping, string>(settings.TeamBacklogMappings, x =>  x.TeamId)
			.Distinct()
			.Select(x => new TeamContext(projectId, x))
			.ToList();
		var getIterationsTasks = teamsContexts.Select(teamContext => workClient.GetTeamIterationsAsync(teamContext))
			.ToArray();
		await Task.WhenAll(getIterationsTasks);
		var iterations = getIterationsTasks.Select(t => t.Result)
			.SelectMany(list => list)
			.DistinctBy(x => x.Id)
			.ToList();
		var allProjectTeams = await teamHttpClient.GetTeamsAsync(projectId, expandIdentity: true);
		var teams = allProjectTeams.Where(team => teamsContexts.Any(teamContext => new Guid(teamContext.Team) == team.Id))
			.ToList();

		var teamFieldValuesTasks = teamsContexts.Select(teamContext => workClient.GetTeamFieldValuesAsync(teamContext));
		var teamFieldValues = await Task.WhenAll(teamFieldValuesTasks);
		var teamAreas = Enumerable.Zip(teamFieldValues, teamsContexts)
			.ToDictionary(pair => pair.Second.Team, pair => pair.First.Values.ToList());

		return new RoadmapDefinition
		{
			ProjectId = projectId,
			RoadmapId = planId,
			PlanSettings = settings,
			Iterations = iterations,
			Teams = teams,
			AreasByTeam = teamAreas
		};
	}

	public static async Task<RoadmapEntries> GetRoadmapEntries(VssConnection connection, RoadmapDefinition roadmapDefinition, List<string> types)
	{
		var witHttpClient = connection.GetClient<WorkItemTrackingHttpClient>();
		var areas = roadmapDefinition.AreasByTeam.Values
			.SelectMany(list => list)
			.Select(value => $"'{value.Value}'")
			.Distinct();
		types = types.Select(type => $"'{type}'")
			.ToList();
		string query = $"SELECT [System.Id] " +
		               $"FROM WorkItems " +
		               $"WHERE [System.WorkItemType] IN ({string.Join(", ", types)}) " +
		               $"AND [System.AreaPath] IN ({string.Join(", ", areas)})";

		WorkItemQueryResult? queryResults = await witHttpClient.QueryByWiqlAsync(new Wiql() { Query = query }, roadmapDefinition.ProjectId);

		int[] workItemIds = queryResults.WorkItems.Select(x => x.Id).ToArray();
		var getWorkItemsTasks = new List<Task<List<WorkItem>>>();

		const int batchSize = 200;
		for (var i = 0; i < workItemIds.Length; i += batchSize)
		{
			int[] idsBatch = workItemIds.Skip(i).Take(batchSize).ToArray();
			getWorkItemsTasks.Add(witHttpClient.GetWorkItemsAsync(idsBatch));
		}

		var batchedWorkItems = await Task.WhenAll(getWorkItemsTasks);
		var workItems = batchedWorkItems.SelectMany(x => x);
		var columns = CalculateColumns(roadmapDefinition.Iterations);

		var roadmapWorkItems = workItems.Select(x => new RoadmapWorkItem()
		{
			Title = x.Fields.TryGetWithDefault("System.Title", "UNKNOWN"),
			State = x.Fields.TryGetWithDefault("System.State", "UNKNOWN"),
			Type = x.Fields.TryGetWithDefault("System.WorkItemType", "UNKNOWN"),
			AssignedTo = x.Fields.TryGetWithDefault<string, IdentityRef?>("System.AssignedTo", null)?.DisplayName ?? "",
			Iteration = FindColumnForWorkItem(x, columns)
		}).ToList();

		return new RoadmapEntries()
		{
			Iterations = columns,
			RoadmapWorkItems = roadmapWorkItems
		};
	}

	private static Iteration FindColumnForWorkItem(WorkItem workItem, List<Iteration> columns)
	{
		string? iterationPath = workItem.Fields.TryGetWithDefault<string, string?>("System.IterationPath", null);
		if (iterationPath == null)
		{
			return columns.First();
		}

		Iteration? bestCandidate = null;
		foreach (Iteration column in columns)
		{
			if (iterationPath.StartsWith(column.IterationPath) && (bestCandidate == null || bestCandidate.IterationPath.Length < column.IterationPath.Length))
			{
				bestCandidate = column;
			}
		}

		return bestCandidate ?? columns.First();
	}

	private static List<Iteration> CalculateColumns(List<TeamSettingsIteration> iterations)
	{
		return
			iterations.Where(x => x.Attributes.StartDate != null && x.Attributes.FinishDate != null)
				.Select(x => new Iteration()
				{
					StartDate = x.Attributes.StartDate!.Value,
					EndDate = x.Attributes.FinishDate!.Value,
					IterationPath = x.Path,
					DisplayName = x.Name,
				})
				.ToList();
	}

}