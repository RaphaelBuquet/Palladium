using Microsoft.Reactive.Testing;
using Palladium.BuiltinActions.SearchOverride;

namespace Palladium.BuiltinActions.Tests;

public class WindowsKeyboardTests
{
	[Test]
	public void ForTwoKeysShortcut_PressKeyOne_ThenRelease_ThenPressKeyTwo_DoesNotInvokeCallback()
	{
		// arrange
		var scheduler = new TestScheduler();
		var called = false;
		Action callback = void () => called = true;
		var windowsKeyboard = new WindowsKeyboard();
		uint keyOne = 1;
		uint? keyTwo = 2;
		uint? keyThree = null;

		// act
		bool call1 = windowsKeyboard.ProcessKey(WindowsKeyboard.WM_KEYDOWN, keyOne, keyTwo, keyThree, scheduler, callback, keyOne);
		bool call2 = windowsKeyboard.ProcessKey(WindowsKeyboard.WM_KEYUP, keyOne, keyTwo, keyThree, scheduler, callback, keyOne);
		bool call3 = windowsKeyboard.ProcessKey(WindowsKeyboard.WM_KEYDOWN, keyOne, keyTwo, keyThree, scheduler, callback, keyTwo.Value);
		scheduler.AdvanceBy(1);
		
		// assert
		Assert.IsTrue(call1);
		Assert.IsTrue(call2);
		Assert.IsTrue(call3);
		Assert.IsFalse(called);
	}
	
	[Test]
	public void ForTwoKeysShortcut_PressKeyOne_ThenPressTwo_InvokesCallback()
	{
		// arrange
		var scheduler = new TestScheduler();
		var called = false;
		Action callback = void () => called = true;
		var windowsKeyboard = new WindowsKeyboard();
		uint keyOne = 1;
		uint? keyTwo = 2;
		uint? keyThree = null;

		// act
		bool call1 = windowsKeyboard.ProcessKey(WindowsKeyboard.WM_KEYDOWN, keyOne, keyTwo, keyThree, scheduler, callback, keyOne);
		bool call2 = windowsKeyboard.ProcessKey(WindowsKeyboard.WM_KEYDOWN, keyOne, keyTwo, keyThree, scheduler, callback, keyTwo.Value);
		scheduler.AdvanceBy(1);

		// assert
		Assert.IsTrue(call1);
		Assert.IsFalse(call2);
		Assert.IsTrue(called);
	}
}