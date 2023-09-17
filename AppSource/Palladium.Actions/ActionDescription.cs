using Avalonia.Controls;

namespace Palladium.ActionsService;

public class ActionDescription
{
	/// <summary>
	///     Unique identifier.
	/// </summary>
	public Guid Guid;

	/// <summary>
	///     User-facing icon.
	/// </summary>
	public string? Emoji;

	/// <summary>
	///     User-facing description.
	/// </summary>
	public string? Title;

	/// <summary>
	///     User-facing description.
	/// </summary>
	public string? Description;

	/// <summary>
	///     When true, multiple tabs can be opened for this action.
	/// </summary>
	public bool CanOpenMultiple;

	/// <summary>
	///     <para>
	///         Called when the user starts the action. The given <see cref="ContentControl" /> is the newly created tab. The
	///         UI
	///         can be added inside using <see cref="ContentControl.Content" />.
	///     </para>
	///     <para>
	///         When the tab for the action is closed, the <see cref="ContentControl.Content" /> will be disposed if it
	///         implements <see cref="IDisposable" />.
	///     </para>
	/// </summary>
	public Action<ContentControl>? OnStart;

	public ActionDescription(Guid guid)
	{
		Guid = guid;
	}
}