using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using DynamicData;
using DynamicData.Binding;
using Palladium.ActionsService;
using Palladium.Settings;
using ReactiveUI;

namespace Palladium.BuiltinActions.Settings;

public class SettingsViewModel : IActivatableViewModel
{
	public SettingsViewModel(ActionsRepositoryService actionsRepositoryService, SettingsService settingsService)
	{
		this.WhenActivated( disposables =>
		{
			Disposable.Create(() => Settings.Clear()).DisposeWith(disposables);
			
			settingsService.SettingsViews
				.Connect()
				.InnerJoin(
					actionsRepositoryService.Actions.Connect(),
					actionDescription => actionDescription.Guid,
					(tuple, description) => (description, tuple.CreateView))
				.ObserveOn(RxApp.MainThreadScheduler)
				.Transform(tuple => new SettingsEntryViewModel(
					tuple.description.Title ?? "",
					$"{tuple.description.Emoji} {tuple.description.Title}", tuple.CreateView.Invoke()))
				.Bind(Settings)
				.Subscribe()
				.DisposeWith(disposables);
		});
	}

	public SettingsViewModel()
	{
		HandleDesignMode();
	}

	public ObservableCollectionExtended<SettingsEntryViewModel> Settings { get; } = new ();

	/// <inheritdoc />
	ViewModelActivator IActivatableViewModel.Activator { get; } = new ();

	private void HandleDesignMode()
	{
		if (Design.IsDesignMode)
		{
			Settings.Add(new SettingsEntryViewModel("Palladium Application", "⚙️ Palladium Application", new StackPanel
			{
				Spacing = 6,
				Orientation = Orientation.Horizontal,
				Children =
				{
					new ToggleButton(),
					new TextBlock { Text = "Launch at startup" }
				}
			})
			{
				TitleFontWeight = FontWeight.Bold
			});
			Settings.Add(new SettingsEntryViewModel("Search Override", "🔎 Search Override", "Not implemented."));
		}
	}
}