using DynamicData;

namespace Palladium.ActionsService;

public class ActionsRepositoryService
{
	public readonly SourceList<ActionDescription> Actions = new ();
}