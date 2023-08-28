using DynamicData;
using Palladium.ActionsService;
using Palladium.ViewModels;

namespace Palladium.Tests;

public class HomeViewModelTests
{
	[Test]
	public void HomeViewModelActions_ReflectActions()
	{
		// arrange
		var actions = new ActionsRepositoryService();
		var tabsService = new TabsService();
		var vm = new HomeViewModel(actions, tabsService);
		
		// act
		actions.Actions.Add(new ActionDescription(){ Title = "1"});
		
		// assert
		Assert.AreEqual(1, vm.Actions.Count);
		Assert.AreEqual("1", vm.Actions.Last().Title);
		
		// act
		Task.Run(() =>
		{
			actions.Actions.Add(new ActionDescription() { Title = "2" });
		}).Wait();
		
		// assert
		Assert.AreEqual(2, vm.Actions.Count);
		Assert.AreEqual("2", vm.Actions.Last().Title);
	}
}