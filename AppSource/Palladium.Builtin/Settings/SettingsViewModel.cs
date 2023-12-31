﻿using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using DynamicData;
using DynamicData.Binding;
using Palladium.Settings;
using ReactiveUI;

namespace Palladium.Builtin.Settings;

public class SettingsViewModel : IActivatableViewModel
{
	public SettingsViewModel(SettingsService settingsService)
	{
		this.WhenActivated( disposables =>
		{
			Disposable.Create(() => Settings.Clear()).DisposeWith(disposables);
			settingsService.SettingsViews
				.Connect()
				.Transform(tuple => new SettingsEntryViewModel(
					tuple.Text.Title ?? "Unknown",
					tuple.Text.SectionTitle ?? $"Unknown {tuple.Guid}",
					tuple.CreateView.Invoke()))
				.ObserveOn(RxApp.MainThreadScheduler)
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
					new CheckBox(),
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