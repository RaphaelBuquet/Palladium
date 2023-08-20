using System.Reactive;
using Avalonia.Controls;
using Palladium.Tabs;
using ReactiveUI;

namespace Palladium.ActionsService.ViewModels;

public class ActionViewModel
{
	/// <summary>
	///     User-facing icon.
	/// </summary>
	public string? Emoji { get; set; }

	/// <summary>
	///     User-facing description.
	/// </summary>
	public string? Title { get; set; }

	/// <summary>
	///     User-facing description.
	/// </summary>
	public string? Description { get; set; }

	/// <summary>
	///     Called when the user starts the action. The given <see cref="ContentControl" /> is the newly created tab. The UI
	///     can be added inside using <see cref="ContentControl.Content" />.
	/// </summary>
	public ReactiveCommand<Unit, Unit>? StartCommand { get; set; }

	public ActionViewModel()
	{ }

	public ActionViewModel(ActionDescription description, TabsService tabsService)
	{
		Emoji = description.Emoji;
		Title = description.Title;
		Description = description.Description;
		StartCommand = ReactiveCommand.Create(() =>
		{
			description.OnStart.Invoke(tabsService.AddNewTab($"{description.Emoji} {description.Title}"));
		});
	}
}