using Avalonia.Controls;
using NSubstitute;
using Palladium.ActionsService;
using Palladium.Controls;

namespace Palladium.Actions.Tests;

public class TabsServiceTests
{
	[Test]
	public void AddAndRemove()
	{
		// arrange
		var mock = Substitute.For<IDisposable>();
		var tabControl = new TabControl();
		var tabService = new TabsService
		{
			Target = tabControl
		};

		// act
		tabService.HandleStartAction(new ActionDescription(new Guid("710F4DED-98D5-4044-BA81-C4A687486A7F"))
		{
			Title = "Test",
			OnStart = control => { control.Content = mock; }
		});

		// assert
		Assert.AreEqual(1, tabControl.Items.Count);

		// act
		ApplicationTabItem tabItem = tabControl.Items.OfType<ApplicationTabItem>().First();
		tabItem.CloseTabCommand!.Execute(null);

		// assert
		mock.Received().Dispose();
		Assert.AreEqual(0, tabControl.Items.Count);
	}
}