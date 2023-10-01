using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DynamicData;
using Palladium.ActionsService;
using Palladium.ActionsService.ViewModels;
using Palladium.ViewModels;
using Palladium.Views;
using Action = Palladium.ActionsService.Views.Action;

namespace Palladium.Tests;

[TestFixture]
public class HomeTests
{
	[AvaloniaTest]
	public void ActionsAreDisplayed()
	{
		// arrange
		var home = new Home();
		var window = new Window() { Content = home };
		var actionsRepository = new ActionsRepositoryService();
		var tabs = new TabsService();
		var vm = new HomeViewModel(actionsRepository, tabs);
		home.DataContext = vm;
		window.Show();
		var actionsItemsControl = home.FindDescendantOfType<ItemsControl>();

		// act
		actionsRepository.Actions.AddOrUpdate(new ActionDescription(new Guid("A1E1268F-167D-4AF2-AA40-5F221D0F9CE1")) { Title = "Action1" });
		Dispatcher.UIThread.RunJobs(); // needed for the ItemsControl to populate its children entirely

		// assert
		Assert.IsNotNull(actionsItemsControl);
		Assert.AreEqual(1, actionsItemsControl!.ItemCount);
		ActionViewModel? actionVm = actionsItemsControl.Items.Cast<ActionViewModel>().First();
		Assert.AreEqual("Action1", actionVm!.Title);

		// act
		Task.Run(() => { actionsRepository.Actions.AddOrUpdate(new ActionDescription(new Guid("C015DE92-A086-4576-B11C-9494D5174772")) { Title = "Action2" }); }).Wait();
		Dispatcher.UIThread.RunJobs();

		// assert
		Assert.AreEqual(2, actionsItemsControl.ItemCount);

		var actions = actionsItemsControl.GetLogicalDescendants().OfType<Action>();
		Action? match = actions.FirstOrDefault(a => a.DataContext is ActionViewModel { Title: "Action2" } );
		Assert.IsNotNull(match);
	}
}