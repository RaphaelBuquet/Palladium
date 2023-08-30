using Avalonia.Controls.Documents;

namespace Palladium.Controls;

/// <summary>
///     Use this to add line breaks, that are only applied when they are followed by another <see cref="Inline" />.
///     This is useful as adding normal <see cref="LineBreak" /> would increase the height of the <see cref="MiniLog" />.
/// </summary>
/// <remarks>
///     This is meant to be used with a <see cref="MiniLog" />. It will not work with built-in Avalonia controls.
/// </remarks>
public class SmartLineBreak : LineBreak
{
	/// <summary>
	///     Reusable instance to reduce allocations. You can still create a separate instance for styling purposes.
	/// </summary>
	public static readonly SmartLineBreak Instance = new ();
}