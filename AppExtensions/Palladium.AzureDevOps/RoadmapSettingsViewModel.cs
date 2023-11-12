using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.DataProtection;
using Palladium.Settings;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Palladium.AzureDevOps;

public class RoadmapSettingsViewModel :  ReactiveObject, IActivatableViewModel, ISettings<RoadmapSettings>
{
	[Reactive]
	public string? OrganisationUrl { get; set; }

	[Reactive]
	public string? ConnectionToken { get ; set ; }
	
	[Reactive]
	public string? ProjectId { get; set; }

	[Reactive]
	public string? PlanId { get; set; }

	/// <inheritdoc />
	public Guid SettingsGuid => new ("863E717F-C1ED-43CD-A54D-823F0A10BD5B");

	/// <inheritdoc />
	public SettingsText SettingsText => new()
	{
		Title = "Azure DevOps Roadmap Settings",
		SectionTitle = "🗓️ Azure DevOps Roadmap Settings"
	};
	
	public RoadmapSettingsViewModel() : this(null)
	{ }
	
	/// <param name="settingsService"></param>
	/// <inheritdoc />
	public RoadmapSettingsViewModel(ISettingsService? settingsService)
	{
		this.WhenActivated(disposables =>
		{
			Data.Subscribe(x =>
			{
				OrganisationUrl = x.OrganisationUrl;
				ConnectionToken = Decrypt(x.ConnectionTokenEncrypted);
				ProjectId = x.ProjectId;
				PlanId = x.PlanId;
			});

			this.WhenAnyValue(
					x => x.OrganisationUrl, 
					x => x.ConnectionToken,
					x => x.ProjectId,
					x => x.PlanId)
				.Skip(1)
				.Subscribe(_ =>
				{
					Data.OnNext(new RoadmapSettings()
					{
						OrganisationUrl = OrganisationUrl,
						ConnectionTokenEncrypted = Encrypt(ConnectionToken),
						ProjectId = ProjectId,
						PlanId = PlanId
					});
				})
				.DisposeWith(disposables);
			
			Data.Skip(1)
				.DistinctUntilChanged()
				.ObserveOn(RxApp.TaskpoolScheduler)
				.Subscribe(_ => { settingsService?.WriteCommand.Execute().Subscribe(); })
				.DisposeWith(disposables);
		});
	}

	[return: NotNullIfNotNull(nameof(connectionTokenEncrypted))]
	internal static string? Decrypt(string? connectionTokenEncrypted)
	{
		if (connectionTokenEncrypted == null)
		{
			return null;
		}
		
		IDataProtectionProvider dataProtectionProvider = DataProtectionProvider.Create("Palladium");
		IDataProtector dataProtector = dataProtectionProvider.CreateProtector("Token");
		return dataProtector.Unprotect(connectionTokenEncrypted);
	}

	[return: NotNullIfNotNull(nameof(connectionToken))]
	internal static string? Encrypt(string? connectionToken)
	{
		if (connectionToken == null)
		{
			return null;
		}
		
		IDataProtectionProvider dataProtectionProvider = DataProtectionProvider.Create("Palladium");
		IDataProtector dataProtector = dataProtectionProvider.CreateProtector("Token");
		return dataProtector.Protect(connectionToken);
	}

	/// <inheritdoc />
	public BehaviorSubject<RoadmapSettings> Data { get; } = new (new RoadmapSettings());

	/// <inheritdoc />
	public Subject<RoadmapSettings> DeserializedData { get; } = new ();


	/// <inheritdoc />
	ViewModelActivator IActivatableViewModel.Activator { get; } = new ();
}