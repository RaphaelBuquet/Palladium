using System.Diagnostics;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using AzureDevOpsTools;
using ReactiveUI;

namespace Palladium.AzureDevOps;

public class OpenInADOService : IDisposable
{
	private readonly IObservable<string> projectObservable;
	private readonly IObservable<string> organizationUrlObservable;
	private readonly CompositeDisposable disposables = new ();

	/// <summary>
	///     The unified "Open in ADO command".
	/// </summary>
	/// <remarks>
	///     Using a unified command instead of a command on every single work item view model means that clicking on another
	///     item will cancel the task on the previous item.
	/// </remarks>
	public readonly ReactiveCommand<RoadmapWorkItem, Unit> OpenInADOCommand;

	public OpenInADOService(IObservable<string> projectObservable, IObservable<string> organizationUrlObservable)
	{
		this.projectObservable = projectObservable;
		this.organizationUrlObservable = organizationUrlObservable;
		OpenInADOCommand = ReactiveCommand.CreateFromTask<RoadmapWorkItem>(OpenInADO);
	}

	public async Task OpenInADO(RoadmapWorkItem workItem, CancellationToken cancellationToken)
	{
		if (workItem.Id == null)
		{
			return;
		}

		string project = await projectObservable.Take(1).ToTask(cancellationToken);
		string organizationUrl = await organizationUrlObservable.Take(1).ToTask(cancellationToken);

		organizationUrl = organizationUrl.TrimEnd('/');

		string workItemUrl = string.Join("/", organizationUrl, project, "_workitems", "edit", workItem.Id);
		var psi = new ProcessStartInfo()
		{
			FileName = workItemUrl,
			UseShellExecute = true
		};
		Process? process = Process.Start(psi);
		if (process is not null) await process.WaitForExitAsync(cancellationToken);
	}

	/// <inheritdoc />
	public void Dispose()
	{
		disposables.Dispose();
	}
}