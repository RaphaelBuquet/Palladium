using Palladium.ActionsService;
using Palladium.Logging;

namespace Palladium.Extensions;

/// <summary>
///     Create a class that inherits from this. Palladium will set the fields and then call <see cref="Init" />.
/// </summary>
/// <remarks>
///     Fields are used instead of function parameters to simplify porting your extensions to newer versions of Palladium.
/// </remarks>
public abstract class ExtensionBase
{
	public ActionsRepositoryService? ActionsRepositoryService;
	public Log? ApplicationLog;

	/// <summary>
	///     Called by Palladium after the extension's DLL has been loaded. This is called from a background thread.
	/// </summary>
	public abstract void Init();
}