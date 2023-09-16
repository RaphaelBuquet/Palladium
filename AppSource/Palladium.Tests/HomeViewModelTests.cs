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
		actions.Actions.AddOrUpdate(new ActionDescription(new Guid("0E3EDC67-17DC-4A6F-859B-9A68C78434F1")) { Title = "1" });

		// assert
		Assert.AreEqual(1, vm.Actions.Count);
		Assert.AreEqual("1", vm.Actions.Last().Title);

		// act
		Task.Run(() => { actions.Actions.AddOrUpdate(new ActionDescription(new Guid("1A1C4C9B-6B70-41B2-8394-90E569FFFDEC")) { Title = "2" }); }).Wait();

		// assert
		Assert.AreEqual(2, vm.Actions.Count);
		Assert.AreEqual("2", vm.Actions.Last().Title);
	}
}