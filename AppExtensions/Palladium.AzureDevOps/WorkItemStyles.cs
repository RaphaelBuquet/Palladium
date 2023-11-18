using Avalonia.Media;
using AzureDevOpsTools;
using Microsoft.TeamFoundation.WorkItemTracking.Process.WebApi.Models;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace Palladium.AzureDevOps;

public class WorkItemStyles
{
	public static readonly WorkItemStyles Empty = new WorkItemStyles()
	{
		StateToColour = new Dictionary<WorkItemState, Color>(),
		TypeToColour = new Dictionary<string, Color>()
	};

	public required IReadOnlyDictionary<string, Color> TypeToColour;
	public required IReadOnlyDictionary<WorkItemState, Color> StateToColour;

	public static Dictionary<WorkItemState, Color> BuildStateColourLookup(Dictionary<WorkItemState, WorkItemStateColor> colors)
	{
		return colors.ToDictionary(x => x.Key, x => Color.Parse($"#{x.Value.Color}"));
	}

	public static IReadOnlyDictionary<string, Color> BuildTypeColourLookup(List<WorkItemTypeModel> workItemTypes)
	{
		return workItemTypes.ToDictionary(x => x.Name, x => Color.Parse($"#{x.Color}"));
	}
}