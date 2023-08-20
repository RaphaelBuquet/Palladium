using Avalonia.Controls;

namespace Palladium.ActionsService;

public struct ActionDescription
{
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
	///     Called when the user starts the action. The given <see cref="ContentControl" /> is the newly created tab. The UI
	///     can be added inside using <see cref="ContentControl.Content" />.
	/// </summary>
	public Action<ContentControl> OnStart;
}