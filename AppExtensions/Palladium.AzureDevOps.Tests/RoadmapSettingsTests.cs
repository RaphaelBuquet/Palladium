using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;

namespace Palladium.AzureDevOps.Tests;

public class RoadmapSettingsTests
{
	[AvaloniaTest]
	public void ChangeTextUpdatesSettings()
	{
		// arrange
		var window = new Window();
		var vm = new RoadmapSettingsViewModel(null);
		var view = new RoadmapSettingsView
		{
			DataContext = vm
		};
		window.Content = view;
		window.Show();
		Dispatcher.UIThread.RunJobs();

		// act
		const string url = "https://dev.azure.com/example/";
		view.OrganisationUrl.Text = url;

		// assert
		Assert.AreEqual(url, vm.OrganisationUrl);
		Assert.AreEqual(url, vm.Data.Value.OrganisationUrl);

		// act
		const string token = "hello world";
		view.ConnectionToken.Text = token;

		// assert
		Assert.AreEqual(token, vm.ConnectionToken);
		Assert.IsNotNull(vm.Data.Value.ConnectionTokenEncrypted);

		// act
		const string projectId = "123";
		view.Project.Text = projectId;

		// assert
		Assert.AreEqual(projectId, vm.ProjectId);
		Assert.AreEqual(projectId, vm.Data.Value.ProjectId);

		// act
		const string planId = "567";
		view.Plan.Text = planId;

		// assert
		Assert.AreEqual(planId, vm.PlanId);
		Assert.AreEqual(planId, vm.Data.Value.PlanId);

		// act
		const string queryId = "89";
		view.Query.Text = queryId;

		// assert
		Assert.AreEqual(queryId, vm.QueryId);
		Assert.AreEqual(queryId, vm.Data.Value.QueryId);
	}

	[AvaloniaTest]
	public void ViewUpdatedOnObservableChanges()
	{
		// arrange
		var window = new Window();
		var vm = new RoadmapSettingsViewModel(null);
		var view = new RoadmapSettingsView
		{
			DataContext = vm
		};
		window.Content = view;
		window.Show();
		Dispatcher.UIThread.RunJobs();

		// act
		const string url = "https://dev.azure.com/example/";
		vm.Data.OnNext(vm.Data.Value with { OrganisationUrl = url });

		// assert
		Assert.AreEqual(url, view.OrganisationUrl.Text);

		// act
		const string token = "hello world";
		vm.Data.OnNext(vm.Data.Value with { ConnectionTokenEncrypted = RoadmapSettingsViewModel.Encrypt(token) });

		// assert
		Assert.IsNotNull(token, view.ConnectionToken.Text);

		// act
		const string projectId = "123";
		vm.Data.OnNext(vm.Data.Value with { ProjectId = projectId });

		// assert
		Assert.AreEqual(projectId, view.Project.Text);

		// act
		const string planId = "567";
		vm.Data.OnNext(vm.Data.Value with { PlanId = planId });

		// assert
		Assert.AreEqual(planId, view.Plan.Text);

		// act
		const string queryId = "89";
		vm.Data.OnNext(vm.Data.Value with { QueryId = queryId });

		// assert
		Assert.AreEqual(queryId, view.Query.Text);
	}
}