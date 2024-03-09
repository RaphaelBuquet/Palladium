using System.Diagnostics.CodeAnalysis;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using AzureDevOpsTools;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.Work.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Palladium.ExtensionFunctions;
using Palladium.ExtensionFunctions.Lifecycle;
using Palladium.Logging;
using ReactiveUI;

namespace Palladium.AzureDevOps;

[SuppressMessage("ReSharper", "ConvertToLambdaExpression", Justification = "Ignore this as it can make code less readable.")]
[SuppressMessage("ReSharper", "UseCollectionExpression", Justification = "Ignore this as it can make code less readable.")]
public class RoadmapViewModel : ReactiveObject, IActivatableViewModel, ILifecycleAwareViewModel
{
	private readonly RoadmapSettingsViewModel? settingsViewModel;
	private ObservableAsPropertyHelper<string>? connectionStatus;

	private ObservableAsPropertyHelper<RoadmapGridViewModel>? roadmapGridViewModel;

	private ObservableAsPropertyHelper<string?>? planValidation;
	private ObservableAsPropertyHelper<string?>? queryValidation;
	private ObservableAsPropertyHelper<string?>? projectValidation;
	private ObservableAsPropertyHelper<string?>? workItemStylesValidation;
	private ObservableAsPropertyHelper<Vector>? defaultScrollbarNormalisedPosition;
	private ObservableAsPropertyHelper<bool>? isLoading;
	private double zoomLevel = 1;

	public RoadmapViewModel() : this(null, null)
	{ }

	/// <inheritdoc />
	public RoadmapViewModel(RoadmapSettingsViewModel? settingsViewModel, Log? log)
	{
		if (Design.IsDesignMode)
		{
			HandleDesignMode();
			return;
		}

		Zoom = ReactiveCommand.Create(() => { ZoomLevel += 0.2; });
		Unzoom = ReactiveCommand.Create(() => { ZoomLevel = Math.Max(0.2, ZoomLevel - 0.2); });

		this.settingsViewModel = settingsViewModel;

		var settingsDataObservable = this.settingsViewModel?.Data ?? Observable.Return(new RoadmapSettings());

		var connectionTasks = settingsDataObservable
			// when the user clicks on refresh, push a new value again to kick off the observable chain.
			.CombineLatest(RefreshCommand.StartWith(new Unit()), (settings, _) => settings)
			.Select<RoadmapSettings, Task<VssConnection>?>(settingsData =>
			{
				if (string.IsNullOrWhiteSpace(settingsData.OrganisationUrl) || string.IsNullOrWhiteSpace(settingsData.ConnectionTokenEncrypted))
				{
					return null;
				}

				return AzureQueries.ConnectWithToken(settingsData.OrganisationUrl, RoadmapSettingsViewModel.Decrypt(settingsData.ConnectionTokenEncrypted));
			})
			.Replay(1);
		this.WhenAttached(disposables => { connectionTasks.Connect().DisposeWith(disposables); });

		this.WhenActivated(disposables =>
		{
			connectionStatus = ConnectionStatusObservable(log, connectionTasks)
				.ToProperty(this, x => x.ConnectionStatus)
				.DisposeWith(disposables);

			var connectionObservable = connectionTasks
				.AddTaskCompletion()
				.Where(task => task == null || task.IsCompletedSuccessfully)
				.Select(task => task?.IsCompletedSuccessfully == true ? task.Result : null)
				.Replay(1);
			connectionObservable.Connect().DisposeWith(disposables);

			var projectObservable = ProjectObservable(log, connectionObservable, settingsDataObservable);
			projectObservable.Connect().DisposeWith(disposables);

			projectValidation = projectObservable
				.Select(x => x.Validation)
				.ToProperty(this, x => x.ProjectValidation)
				.DisposeWith(disposables);

			OpenInADOService openInADOService = new OpenInADOService(
				projectObservable
					.Select(x => x.Value)
					.WhereNotNull(),
				connectionObservable
					.WhereNotNull()
					.Select(x => x.Uri.AbsoluteUri)
			).DisposeWith(disposables);

			var planObservable = PlanObservable(log, connectionObservable, projectObservable, settingsDataObservable);
			planObservable.Connect().DisposeWith(disposables);

			planValidation = planObservable
				.Select(x => x.Validation)
				.ToProperty(this, x => x.PlanValidation)
				.DisposeWith(disposables);

			var queryObservable = QueryObservable(log, connectionObservable, projectObservable, settingsDataObservable);
			queryObservable.Connect().DisposeWith(disposables);

			queryValidation = queryObservable
				.Select(x => x.Validation)
				.ToProperty(this, x => x.QueryValidation)
				.DisposeWith(disposables);

			var workItemStylesObservable = WorkItemStylesObservable(log, connectionObservable, projectObservable);
			workItemStylesObservable.Connect().DisposeWith(disposables);

			workItemStylesValidation = workItemStylesObservable
				.Select(x => x.Validation)
				.ToProperty(this, x => x.WorkItemStylesValidation)
				.DisposeWith(disposables);

			var roadmapEntriesObservable = RoadmapEntriesObservable(log, connectionObservable, projectObservable, planObservable, settingsDataObservable)
				.Replay(1);
			roadmapEntriesObservable.Connect().DisposeWith(disposables);

			roadmapGridViewModel = RoadmapGridViewModelObservable(roadmapEntriesObservable, workItemStylesObservable, openInADOService)
				.ToProperty(this, x => x.RoadmapGridViewModel)
				.DisposeWith(disposables);

			defaultScrollbarNormalisedPosition = DefaultScrollbarNormalisedPositionObservable(roadmapEntriesObservable)
				.ToProperty(this, x => x.DefaultScrollbarNormalisedPosition)
				.DisposeWith(disposables);

			isLoading = IsLoadingObservable(connectionTasks, roadmapEntriesObservable)
				.ToProperty(this, x => x.IsLoading)
				.DisposeWith(disposables);
		});
	}

	public Vector DefaultScrollbarNormalisedPosition => defaultScrollbarNormalisedPosition?.Value ?? Vector.Zero;

	public string ConnectionStatus => connectionStatus?.Value ?? string.Empty;

	/// <inheritdoc />
	public ViewModelActivator Activator { get; } = new ();

	public string? PlanValidation => planValidation?.Value;

	public string? QueryValidation => queryValidation?.Value;

	public RoadmapGridViewModel RoadmapGridViewModel => roadmapGridViewModel?.Value ?? RoadmapGridViewModel.Empty();

	public string? WorkItemStylesValidation => workItemStylesValidation?.Value;

	public string? ProjectValidation => projectValidation?.Value;

	public ReactiveCommand<Unit, Unit> RefreshCommand { get; } = ReactiveCommand.Create(() => new Unit());

	public bool IsLoading => isLoading?.Value ?? false;

	public double ZoomLevel
	{
		get => zoomLevel;
		set => this.RaiseAndSetIfChanged(ref zoomLevel, value);
	}

	public ReactiveCommand<Unit, Unit>? Zoom { get; set; }
	public ReactiveCommand<Unit, Unit>? Unzoom { get; set; }

	/// <inheritdoc />
	LifecycleActivator ILifecycleAwareViewModel.Activator { get; } = new ();

	private static IObservable<bool> IsLoadingObservable(IObservable<Task<VssConnection>?> connectionTasks, IObservable<RoadmapEntries?> roadmapEntriesObservable)
	{
		// for this observable, we need a "source" of objects that will turn on the loading indicator
		// and a source of items that will turn it off.
		return connectionTasks
			.Select(task =>
			{
				// no task? nothing is loading.
				// if there is a task, then turn on the loading indicator. 
				// it will be turned off when items have been emitted.
				return task == null ? LoadingIndicator.Hide : LoadingIndicator.Show;
			})
			// whenever a value is emitted, remove the loading indicator
			.Merge(roadmapEntriesObservable.Select(_ => LoadingIndicator.Hide))
			.Select(loadingIndicator => { return loadingIndicator == LoadingIndicator.Show; });
	}

	private static IObservable<Vector> DefaultScrollbarNormalisedPositionObservable(IObservable<RoadmapEntries?> roadmapEntriesObservable)
	{
		return roadmapEntriesObservable
			.WhereNotNull()
			// Only take the initial entry. Any additional value is from the user clicking "refresh",
			// and we don't want to move the scrollbar when the user clicks refresh as the user 
			// could have manually moved the scrollbar.
			.Take(1)
			.Where(entries => entries.Iterations.Count >= 2)
			.Select(entries =>
			{
				DateTime now = DateTime.Now;
				DateTime startDate = entries.Iterations.Min(x => x.StartDate);
				DateTime endDate = entries.Iterations.Max(x => x.EndDate);

				double relativePosition = Maths.InverseLerp(startDate.Ticks, endDate.Ticks, now.Ticks);

				return new Vector(relativePosition, 0);
			});
	}

	private static IObservable<string> ConnectionStatusObservable(Log? log, IConnectableObservable<Task<VssConnection>?> connectionTasks)
	{
		return connectionTasks
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
			});
	}

	private static IConnectableObservable<ValidatedField<string>> ProjectObservable(Log? log, IObservable<VssConnection?> connectionObservable, IObservable<RoadmapSettings> settingsDataObservable)
	{
		return connectionObservable
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
	}

	private static IConnectableObservable<ValidatedField<Plan>> PlanObservable(Log? log, IObservable<VssConnection?> connectionObservable, IConnectableObservable<ValidatedField<string>> projectObservable, IObservable<RoadmapSettings> settingsDataObservable)
	{
		return connectionObservable
			.Zip(projectObservable) // zip instead of CombineLatest, as project will emit after connection emits.
			.CombineLatest(settingsDataObservable)
			.Select(x => (connection: x.First.First, project: x.First.Second, planId: x.Second.PlanId))
			.SelectMany(async tuple =>
			{
				if (tuple.connection == null || !tuple.project.IsValid)
				{
					return new ValidatedField<Plan> { Value = null, Validation = null };
				}
				if (string.IsNullOrWhiteSpace(tuple.planId))
				{
					return new ValidatedField<Plan>
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
						return new ValidatedField<Plan>()
						{
							Value = null,
							Validation = "Plan could not be found."
						};
					}
					return new ValidatedField<Plan>
					{
						Value = plan,
						Validation = null
					};
				}
				catch (Exception e)
				{
					log?.Emit(new EventId(), LogLevel.Information, "Failed to get plan.", e);
					return new ValidatedField<Plan>
					{
						Value = null,
						Validation = "Failed to get plan."
					};
				}
			}).Replay(1);
	}

	private static IConnectableObservable<ValidatedField<QueryHierarchyItem>> QueryObservable(Log? log, IObservable<VssConnection?> connectionObservable, IConnectableObservable<ValidatedField<string>> projectObservable, IObservable<RoadmapSettings> settingsDataObservable)
	{
		return connectionObservable
			.Zip(projectObservable) // zip instead of CombineLatest, as project will emit after connection emits.
			.CombineLatest(settingsDataObservable)
			.Select(x => (connection: x.First.First, project: x.First.Second, queryId: x.Second.QueryId))
			.SelectMany(async tuple =>
			{
				if (tuple.connection == null || !tuple.project.IsValid)
				{
					return new ValidatedField<QueryHierarchyItem> { Value = null, Validation = null };
				}
				if (string.IsNullOrWhiteSpace(tuple.queryId))
				{
					return new ValidatedField<QueryHierarchyItem>
					{
						Value = null,
						Validation = "Add query ID in application settings to display a roadmap."
					};
				}
				try
				{
					QueryHierarchyItem? query = await AzureQueries.GetQuery(tuple.connection, tuple.project.Value!, tuple.queryId);
					if (query == null)
					{
						return new ValidatedField<QueryHierarchyItem>()
						{
							Value = null,
							Validation = "Query could not be found."
						};
					}
					return new ValidatedField<QueryHierarchyItem>
					{
						Value = query,
						Validation = null
					};
				}
				catch (Exception e)
				{
					log?.Emit(new EventId(), LogLevel.Information, "Failed to get query.", e);
					return new ValidatedField<QueryHierarchyItem>
					{
						Value = null,
						Validation = "Failed to get query."
					};
				}
			}).Replay(1);
	}

	private static IConnectableObservable<ValidatedField<WorkItemStyles>> WorkItemStylesObservable(Log? log, IObservable<VssConnection?> connectionObservable, IConnectableObservable<ValidatedField<string>> projectObservable)
	{
		return connectionObservable
			.Zip(projectObservable) // zip instead of CombineLatest, as project will emit after connection emits.
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
					log?.Emit(new EventId(), LogLevel.Information, "Failed to get styles.", e);
					return new ValidatedField<WorkItemStyles>
					{
						Value = null,
						Validation = "Failed to get styles."
					};
				}
			}).Replay(1);
	}

	private static IObservable<RoadmapEntries?> RoadmapEntriesObservable(Log? log, IObservable<VssConnection?> connectionObservable, IConnectableObservable<ValidatedField<string>> projectObservable, IConnectableObservable<ValidatedField<Plan>> planObservable, IObservable<RoadmapSettings> settingsDataObservable)
	{
		return connectionObservable
			// zip instead of CombineLatest, as they will emit when connection emits.
			.Zip(projectObservable, planObservable,
				(connection, project, plan) => (connection, project, plan))
			.CombineLatest(settingsDataObservable, (tuple, roadmapSettings) => (tuple.connection, tuple.project, tuple.plan, roadmapSettings))
			.SelectMany(async tuple =>
			{
				if (tuple.connection == null || !tuple.project.IsValid || !tuple.plan.IsValid || string.IsNullOrEmpty(tuple.roadmapSettings.QueryId))
				{
					return null;
				}
				try
				{
					var roadmapDefinition = await AzureQueries.GetRoadmapDefinition(tuple.connection, tuple.project.Value!, tuple.plan.Value!);
					if (roadmapDefinition == null)
					{
						return null;
					}
					RoadmapEntries roadmapEntries = await AzureQueries.GetRoadmapEntries(tuple.connection, roadmapDefinition.Value, new Guid(tuple.roadmapSettings.QueryId));
					return (RoadmapEntries?)roadmapEntries;
				}
				catch (Exception e)
				{
					log?.Emit(new EventId(), LogLevel.Information, "Failed to get roadmap entries.", e);
					return null;
				}
			});
	}

	private static IObservable<RoadmapGridViewModel> RoadmapGridViewModelObservable(IObservable<RoadmapEntries?> roadmapEntriesObservable, IConnectableObservable<ValidatedField<WorkItemStyles>> workItemStylesObservable, OpenInADOService openInADOService)
	{
		return roadmapEntriesObservable
			// zip instead of CombineLatest, as they will both emit in unison
			.Zip(workItemStylesObservable,
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
					workItemViewModel.OpenTicketCommand = openInADOService.OpenInADOCommand;
				}

				return new RoadmapGridViewModel()
				{
					Columns = iterationsGrid.Columns,
					Rows = iterationsGrid.Rows
						.Concat(workItemsGrid.Rows)
						.ToList(),
					IterationViewModels = iterationsGrid.IterationViewModels,
					WorkItemViewModels = workItemsGrid.WorkItemViewModels
				};
			});
	}

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
					new (new RoadmapWorkItem()
					{
						Id = 1,
						AssignedTo = "John Smith",
						Iteration = m1,
						State = "In progress",
						Title = "Example bug",
						Type = "Bug"
					})
					{
						StartColumnIndex = 0,
						RowIndex = 2,
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

	private enum LoadingIndicator
	{
		Hide = 0,
		Show = 1
	}
}