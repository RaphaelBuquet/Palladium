using DynamicData;

namespace Palladium.ActionsService;

public class ActionsRepositoryService
{
	public readonly SourceCache<ActionDescription, Guid> Actions = new (description => description.Guid);
}