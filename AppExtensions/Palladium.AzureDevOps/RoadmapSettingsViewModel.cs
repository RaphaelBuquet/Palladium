using System.Reactive.Subjects;
using Palladium.Settings;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Palladium.AzureDevOps;

public class RoadmapSettingsViewModel :  ReactiveObject, IActivatableViewModel, ISettings<RoadmapSettings>
{
	[Reactive]
	public string OrganisationUrl { get; set; } = "";

	/// <inheritdoc />
	public Guid SettingsGuid => new ("B664130A-E825-4FC2-8CF1-3C3DB9FF505");

	/// <inheritdoc />
	public SettingsText SettingsText => new()
	{
		Title = "Azure DevOps Roadmap Settings",
		SectionTitle = "🗓️ Azure DevOps Roadmap Settings"
	};

	/// <inheritdoc />
	public BehaviorSubject<RoadmapSettings> Data { get; } = new (new RoadmapSettings());

	/// <inheritdoc />
	public Subject<RoadmapSettings> DeserializedData { get; } = new ();


	/// <inheritdoc />
	ViewModelActivator IActivatableViewModel.Activator { get; } = new ();
}