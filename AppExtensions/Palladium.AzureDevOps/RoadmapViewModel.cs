using System.Reactive.Disposables;
using System.Reactive.Linq;
using AzureDevOpsTools;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Services.WebApi;
using Palladium.ExtensionFunctions;
using Palladium.Logging;
using ReactiveUI;

namespace Palladium.AzureDevOps;

public class RoadmapViewModel : ReactiveObject, IActivatableViewModel
{
	private readonly RoadmapSettingsViewModel? settings;
	private ObservableAsPropertyHelper<string>? connectionStatus;

	public string ConnectionStatus => connectionStatus?.Value ?? string.Empty;

	public RoadmapViewModel() : this(null, null)
	{ }

	/// <inheritdoc />
	public RoadmapViewModel(RoadmapSettingsViewModel? settings, Log? log)
	{
		this.settings = settings;
		var connectionDataObservable = this.settings?.Data ?? Observable.Return(new RoadmapSettings());
		var connection = connectionDataObservable
			.Select<RoadmapSettings, Task<VssConnection>?>(settingsData =>
			{
				if (string.IsNullOrWhiteSpace(settingsData.OrganisationUrl) || string.IsNullOrWhiteSpace(settingsData.ConnectionTokenEncrypted))
				{
					return null;
				}

				return AzureQueries.ConnectWithToken(settingsData.OrganisationUrl, RoadmapSettingsViewModel.Decrypt(settingsData.ConnectionTokenEncrypted));
			}).Replay(1);
		connection.Connect();

		this.WhenActivated(disposables =>
		{
			connectionStatus = connection
				.AddTaskCompletion()
				.Select(task =>
				{
					if (task == null)
					{
						return "Add credentials in application settings to connect to Azure DevOps.";
					}
					if (task.IsCompleted)
					{
						if (task.IsCompletedSuccessfully)
						{
							return "Connected.";
						}
						if (task.Exception != null)
						{
							log?.Emit(new EventId(), LogLevel.Information, "Connection to Azure DevOps failed.", task.Exception);
						}
						return "Connection failed.";
					}
					return "Connecting...";
				}).ToProperty(this, x => x.ConnectionStatus)
				.DisposeWith(disposables);
		});
	}

	/// <inheritdoc />
	public ViewModelActivator Activator { get; } = new ();
}