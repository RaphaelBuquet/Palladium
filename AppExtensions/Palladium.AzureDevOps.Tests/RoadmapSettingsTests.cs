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
		var vm = new RoadmapSettingsViewModel();
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
	}
}