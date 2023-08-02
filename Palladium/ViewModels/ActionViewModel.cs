using System.Reactive;
using ReactiveUI;

namespace Palladium.ViewModels;

public class ActionViewModel : ViewModelBase
{
	/// <inheritdoc />
	public ActionViewModel()
	{ }

	public char Icon { get; set; }
	public string Title { get; set; } = "";
	public string Description { get; set; } = "";
	public ReactiveCommand<Unit, Unit>? RunActionCommand { get; set; }
}