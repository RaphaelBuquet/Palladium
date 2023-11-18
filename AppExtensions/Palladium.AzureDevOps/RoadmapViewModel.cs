using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Media;
using AzureDevOpsTools;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.Work.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Palladium.ExtensionFunctions;
using Palladium.ExtensionFunctions.Lifecycle;
using Palladium.Logging;
using ReactiveUI;

namespace Palladium.AzureDevOps;

public class RoadmapViewModel : ReactiveObject, IActivatableViewModel, ILifecycleAwareViewModel
{
	private readonly RoadmapSettingsViewModel? settings;
	private ObservableAsPropertyHelper<string>? connectionStatus;

	private ObservableAsPropertyHelper<RoadmapGridViewModel>? roadmapGridViewModel;

	private ObservableAsPropertyHelper<string?>? planValidation;

	private ObservableAsPropertyHelper<string?>? projectValidation;

	private ObservableAsPropertyHelper<string?>? workItemStylesValidation;

	public RoadmapViewModel() : this(null, null)
	{ }

	/// <inheritdoc />
	public RoadmapViewModel(RoadmapSettingsViewModel? settings, Log? log)
	{
		if (Design.IsDesignMode)
		{
			HandleDesignMode();
			return;
		}

		this.settings = settings;

		var settingsDataObservable = this.settings?.Data ?? Observable.Return(new RoadmapSettings());

		var connectionTasks = settingsDataObservable
			.Select<RoadmapSettings, Task<VssConnection>?>(settingsData =>
			{
				if (string.IsNullOrWhiteSpace(settingsData.OrganisationUrl) || string.IsNullOrWhiteSpace(settingsData.ConnectionTokenEncrypted))
				{
					return null;
				}

				return AzureQueries.ConnectWithToken(settingsData.OrganisationUrl, RoadmapSettingsViewModel.Decrypt(settingsData.ConnectionTokenEncrypted));
			}).Replay(1);
		this.WhenAttached(disposables => { connectionTasks.Connect().DisposeWith(disposables); });

		this.WhenActivated(disposables =>
		{
			connectionStatus = connectionTasks
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
							if (task.Exception.InnerExceptions.First() is VssUnauthorizedException)
							{
								return "Connection failed: the token is invalid.";
							}
						}
						return "Connection failed.";
					}
					return "Connecting...";
				}).ToProperty(this, x => x.ConnectionStatus)
				.DisposeWith(disposables);

			var connectionObservable = connectionTasks
				.AddTaskCompletion()
				.Where(task => task == null || task.IsCompletedSuccessfully)
				.Select(task => task?.IsCompletedSuccessfully == true ? task.Result : null);

			var projectObservable = connectionObservable
				.CombineLatest(settingsDataObservable)
				.Select(x => (connection: x.First, projectId: x.Second.ProjectId))
				.SelectMany(async tuple =>
				{
					if (tuple.connection == null)
					{
						return new ValidatedField<string> { Value = null, Validation = null };
					}
					if (string.IsNullOrWhiteSpace(tuple.projectId))
					{
						return new ValidatedField<string>
						{
							Value = null,
							Validation = "Add project ID in application settings to display a roadmap."
						};
					}
					try
					{
						TeamProject? project = await AzureQueries.GetProject(tuple.connection, tuple.projectId);
						if (project == null)
						{
							return new ValidatedField<string>()
							{
								Value = null,
								Validation = "Project could not be found."
							};
						}
						return new ValidatedField<string>
						{
							Value = tuple.projectId,
							Validation = null
						};
					}
					catch (Exception e)
					{
						log?.Emit(new EventId(), LogLevel.Information, "Failed to get project.", e);
						return new ValidatedField<string>
						{
							Value = null,
							Validation = "Failed to get project."
						};
					}
				}).Replay(1);
			projectObservable.Connect().DisposeWith(disposables);

			projectValidation = projectObservable
				.Select(x => x.Validation)
				.ToProperty(this, x => x.ProjectValidation)
				.DisposeWith(disposables);

			var planObservable = connectionObservable
				.CombineLatest(projectObservable)
				.CombineLatest(settingsDataObservable)
				.Select(x => (connection: x.First.First, project: x.First.Second, planId: x.Second.PlanId))
				.SelectMany(async tuple =>
				{
					if (tuple.connection == null || !tuple.project.IsValid)
					{
						return new ValidatedField<string> { Value = null, Validation = null };
					}
					if (string.IsNullOrWhiteSpace(tuple.planId))
					{
						return new ValidatedField<string>
						{
							Value = null,
							Validation = "Add plan ID in application settings to display a roadmap."
						};
					}
					try
					{
						Plan? plan = await AzureQueries.GetPlan(tuple.connection, tuple.project.Value!, tuple.planId);
						if (plan == null)
						{
							return new ValidatedField<string>()
							{
								Value = null,
								Validation = "Plan could not be found."
							};
						}
						return new ValidatedField<string>
						{
							Value = tuple.planId,
							Validation = null
						};
					}
					catch (Exception e)
					{
						log?.Emit(new EventId(), LogLevel.Information, "Failed to get plan.", e);
						return new ValidatedField<string>
						{
							Value = null,
							Validation = "Failed to get plan."
						};
					}
				}).Replay(1);
			planObservable.Connect().DisposeWith(disposables);

			planValidation = planObservable
				.Select(x => x.Validation)
				.ToProperty(this, x => x.PlanValidation)
				.DisposeWith(disposables);

			var workItemStylesObservable = connectionObservable
				.CombineLatest(projectObservable)
				.Select(x => (connection: x.First, project: x.Second))
				.SelectMany(async tuple =>
				{
					if (tuple.connection == null || !tuple.project.IsValid)
					{
						return new ValidatedField<WorkItemStyles> { Value = null, Validation = null };
					}
					try
					{
						var workItemTypes = await AzureQueries.GetWorkItemTypes(tuple.connection, tuple.project.Value!);
						var stateColorsTask = await AzureQueries.GetStateColors(tuple.connection, workItemTypes, tuple.project.Value!);

						var styles = new WorkItemStyles()
						{
							StateToColour = WorkItemStyles.BuildStateColourLookup(stateColorsTask),
							TypeToColour = WorkItemStyles.BuildTypeColourLookup(workItemTypes)
						};

						return new ValidatedField<WorkItemStyles>
						{
							Value = styles,
							Validation = null
						};
					}
					catch (Exception e)
					{
						log?.Emit(new EventId(), LogLevel.Information, "Failed to get plan.", e);
						return new ValidatedField<WorkItemStyles>
						{
							Value = null,
							Validation = "Failed to get plan."
						};
					}
				}).Replay(1);
			workItemStylesObservable.Connect().DisposeWith(disposables);

			workItemStylesValidation = workItemStylesObservable
				.Select(x => x.Validation)
				.ToProperty(this, x => x.WorkItemStylesValidation)
				.DisposeWith(disposables);

			var roadmapEntriesObservable = connectionObservable
				.CombineLatest(
					projectObservable,
					planObservable,
					(connection, project, plan)
						=> (connection, project, plan))
				.SelectMany(async tuple =>
				{
					if (tuple.connection == null || !tuple.project.IsValid || !tuple.plan.IsValid)
					{
						return null;
					}
					try
					{
						var roadmapTypes = await AzureQueries.GetAutomaticRoadmapTypes(tuple.connection, tuple.project.Value!);
						RoadmapDefinition roadmapDefinition = await AzureQueries.GetRoadmapDefinition(tuple.connection, tuple.project.Value!, tuple.plan.Value!);
						RoadmapEntries roadmapEntries = await AzureQueries.GetRoadmapEntries(tuple.connection, roadmapDefinition, roadmapTypes);
						return (RoadmapEntries?)roadmapEntries;
					}
					catch (Exception)
					{
						return null;
					}
				});

			roadmapGridViewModel = roadmapEntriesObservable
				.CombineLatest(workItemStylesObservable,
					(roadmapEntries, workItemStyles)
						=> (roadmapEntries, workItemStyles))
				.Select(tuple =>
				{
					if (tuple.roadmapEntries == null || !tuple.workItemStyles.IsValid)
					{
						return RoadmapGridViewModel.Empty();
					}
					RoadmapGridAlgorithms.IterationsGrid iterationsGrid =
						RoadmapGridAlgorithms.CreateIterationsGrid(tuple.roadmapEntries.Value.Iterations);
					RoadmapGridAlgorithms.WorkItemGrid workItemsGrid =
						RoadmapGridAlgorithms.CreateWorkItemsGrid(iterationsGrid.Rows.Count, iterationsGrid.IterationViewModels, tuple.roadmapEntries.Value.RoadmapWorkItems);

					foreach (WorkItemViewModel workItemViewModel in workItemsGrid.WorkItemViewModels)
					{
						workItemViewModel.WorkItemStyles = tuple.workItemStyles.Value;
					}

					return new RoadmapGridViewModel()
					{
						Columns = iterationsGrid.Columns,
						Rows = iterationsGrid.Rows.Concat(workItemsGrid.Rows).ToList(),
						IterationViewModels = iterationsGrid.IterationViewModels,
						WorkItemViewModels = workItemsGrid.WorkItemViewModels
					};
				}).ToProperty(this, x => x.RoadmapGridViewModel)
				.DisposeWith(disposables);
		});
	}

	public string ConnectionStatus => connectionStatus?.Value ?? string.Empty;

	/// <inheritdoc />
	public ViewModelActivator Activator { get; } = new ();

	public string? PlanValidation => planValidation?.Value;

	public RoadmapGridViewModel RoadmapGridViewModel => roadmapGridViewModel?.Value ?? RoadmapGridViewModel.Empty();

	public string? WorkItemStylesValidation => workItemStylesValidation?.Value;

	public string? ProjectValidation => projectValidation?.Value;

	/// <inheritdoc />
	LifecycleActivator ILifecycleAwareViewModel.Activator { get; } = new ();

	private void HandleDesignMode()
	{
		var m1 = new Iteration
		{
			DisplayName = "M1",
			StartDate = new DateTime(2023, 11, 1),
			EndDate = new DateTime(2023, 11, 30),
			IterationPath = "Palladium\\M1"
		};
		var m2 = new Iteration
		{
			DisplayName = "M2",
			StartDate = new DateTime(2023, 12, 1),
			EndDate = new DateTime(2023, 12, 31),
			IterationPath = "Palladium\\M2"
		};

		var styles = new WorkItemStyles()
		{
			StateToColour = new Dictionary<WorkItemState, Color>
			{
				{ new WorkItemState() { State = "In progress", WorkItemType = "Bug" }, Colors.CornflowerBlue }
			},
			TypeToColour = new Dictionary<string, Color>()
			{
				{ "Bug", Colors.OrangeRed }
			}
		};

		roadmapGridViewModel = Observable.Return(new RoadmapGridViewModel()
			{
				IterationViewModels = new List<IterationViewModel>()
				{
					new (m1)
					{
						StartColumnIndex = 0,
						RowIndex = 0,
						EndColumnIndexExclusive = 1
					},
					new (m2)
					{
						StartColumnIndex = 2,
						RowIndex = 0,
						EndColumnIndexExclusive = 3
					}
				},
				WorkItemViewModels = new List<WorkItemViewModel>()
				{
					new WorkItemViewModel(new RoadmapWorkItem()
					{
						AssignedTo = "John Smith",
						Iteration = m1,
						State = "In progress",
						Title = "Example bug",
						Type = "Bug"
					})
					{
						StartColumnIndex = 0,
						RowIndex = 1,
						EndColumnIndexExclusive = 1,
						WorkItemStyles = styles
					}
				},
				Columns = new List<GridLength>()
				{
					new (30, GridUnitType.Star),
					new (1, GridUnitType.Star),
					new (30, GridUnitType.Star)
				},
				Rows = new List<GridLength>() { GridLength.Auto, GridLength.Auto }
			})
			.ToProperty(this, x => x.RoadmapGridViewModel);
	}

	private struct ValidatedField<T>
	{
		public required T? Value;
		public required string? Validation;
		public bool IsValid => Value != null && Validation == null;
	}
}