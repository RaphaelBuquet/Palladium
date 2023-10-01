using Microsoft.Reactive.Testing;
using Palladium.Builtin.SearchOverride;

namespace Palladium.Builtin.Tests;

public class WindowsKeyboardTests
{
	[Test]
	public void PressKeysAndModifierSeparately_DoesNotInvokeCallback()
	{
		// arrange
		var scheduler = new TestScheduler();
		var called = false;
		Action callback = void () => called = true;
		var windowsKeyboard = new WindowsKeyboard();
		uint key = 1;
		uint modifier = 2;

		// act
		Assert.IsTrue(windowsKeyboard.ProcessKeyBlocking(WindowsKeyboard.WM_KEYDOWN, key, key, modifier, scheduler, callback));
		Assert.IsTrue(windowsKeyboard.ProcessKeyBlocking(WindowsKeyboard.WM_KEYUP, key, key, modifier, scheduler, callback));
		Assert.IsTrue(windowsKeyboard.ProcessKeyBlocking(WindowsKeyboard.WM_KEYDOWN, modifier, key, modifier, scheduler, callback));
		Assert.IsTrue(windowsKeyboard.ProcessKeyBlocking(WindowsKeyboard.WM_KEYUP, modifier, key, modifier, scheduler, callback));
		scheduler.AdvanceBy(1);

		// assert
		Assert.IsFalse(called);
	}

	[Test]
	public void PressKeyThenPressModifier_DoesNotInvokeCallback()
	{
		// arrange
		var scheduler = new TestScheduler();
		var called = false;
		Action callback = void () => called = true;
		var windowsKeyboard = new WindowsKeyboard();
		uint key = 1;
		uint modifier = 2;

		// act
		Assert.IsTrue(windowsKeyboard.ProcessKeyBlocking(WindowsKeyboard.WM_KEYDOWN, key, key, modifier, scheduler, callback));
		Assert.IsTrue(windowsKeyboard.ProcessKeyBlocking(WindowsKeyboard.WM_KEYDOWN, modifier, key, modifier, scheduler, callback));
		scheduler.AdvanceBy(1);

		// assert
		Assert.IsFalse(called);
	}

	[Test]
	public void PressModifierThenPressKey_InvokesCallback()
	{
		// arrange
		var scheduler = new TestScheduler();
		var called = false;
		Action callback = void () => called = true;
		var windowsKeyboard = new WindowsKeyboard();
		uint key = 1;
		uint modifier = 2;

		// act
		Assert.IsTrue(windowsKeyboard.ProcessKeyBlocking(WindowsKeyboard.WM_KEYDOWN, modifier, key, modifier, scheduler, callback));
		Assert.IsFalse(windowsKeyboard.ProcessKeyBlocking(WindowsKeyboard.WM_KEYDOWN, key, key, modifier, scheduler, callback));
		scheduler.AdvanceBy(1);

		// assert
		Assert.IsTrue(called);
	}

	[Test]
	public void WhenHolding_DontInvokeCallbackAgain_AndReturnFalseForModifierAndForKey()
	{
		// arrange
		var scheduler = new TestScheduler();
		var calledCount = 0;
		Action callback = void () => calledCount++;
		var windowsKeyboard = new WindowsKeyboard();
		uint key = 1;
		uint modifier = 2;

		// act
		// shortcut
		Assert.IsTrue(windowsKeyboard.ProcessKeyBlocking(WindowsKeyboard.WM_KEYDOWN, modifier, key, modifier, scheduler, callback));
		Assert.IsFalse(windowsKeyboard.ProcessKeyBlocking(WindowsKeyboard.WM_KEYDOWN, key, key, modifier, scheduler, callback));

		// hold
		Assert.IsFalse(windowsKeyboard.ProcessKeyBlocking(WindowsKeyboard.WM_KEYDOWN, modifier, key, modifier, scheduler, callback));
		Assert.IsFalse(windowsKeyboard.ProcessKeyBlocking(WindowsKeyboard.WM_KEYDOWN, key, key, modifier, scheduler, callback));
		Assert.IsFalse(windowsKeyboard.ProcessKeyBlocking(WindowsKeyboard.WM_KEYDOWN, modifier, key, modifier, scheduler, callback));
		Assert.IsFalse(windowsKeyboard.ProcessKeyBlocking(WindowsKeyboard.WM_KEYDOWN, key, key, modifier, scheduler, callback));
		Assert.IsFalse(windowsKeyboard.ProcessKeyBlocking(WindowsKeyboard.WM_KEYDOWN, modifier, key, modifier, scheduler, callback));
		Assert.IsFalse(windowsKeyboard.ProcessKeyBlocking(WindowsKeyboard.WM_KEYDOWN, key, key, modifier, scheduler, callback));
		scheduler.AdvanceBy(1);

		// assert
		Assert.AreEqual(1, calledCount);
	}

	[Test]
	public void WhenReleasing_KeyUpReturnsTrue_ModifierUpReturnsTrue()
	{
		// arrange
		var scheduler = new TestScheduler();
		var calledCount = 0;
		Action callback = void () => calledCount++;
		var windowsKeyboard = new WindowsKeyboard();
		uint key = 1;
		uint modifier = 2;

		// act
		// shortcut
		Assert.IsTrue(windowsKeyboard.ProcessKeyBlocking(WindowsKeyboard.WM_KEYDOWN, modifier, key, modifier, scheduler, callback));
		Assert.IsFalse(windowsKeyboard.ProcessKeyBlocking(WindowsKeyboard.WM_KEYDOWN, key, key, modifier, scheduler, callback));

		// release key
		Assert.IsTrue(windowsKeyboard.ProcessKeyBlocking(WindowsKeyboard.WM_KEYUP, key, key, modifier, scheduler, callback));

		// hold modifier 
		Assert.IsFalse(windowsKeyboard.ProcessKeyBlocking(WindowsKeyboard.WM_KEYDOWN, modifier, key, modifier, scheduler, callback));

		// release modifier
		Assert.IsTrue(windowsKeyboard.ProcessKeyBlocking(WindowsKeyboard.WM_KEYUP, modifier, key, modifier, scheduler, callback));

		// press modifier
		Assert.IsTrue(windowsKeyboard.ProcessKeyBlocking(WindowsKeyboard.WM_KEYDOWN, modifier, key, modifier, scheduler, callback));

		// assert
		scheduler.AdvanceBy(1);
		Assert.AreEqual(1, calledCount);
	}

	[Test]
	public void AfterShortcutPressed_AndHold_ThenKeyReleased_ThenPressedAgain_InvokesCallbackAgain()
	{
		// arrange
		var scheduler = new TestScheduler();
		var calledCount = 0;
		Action callback = void () => calledCount++;
		var windowsKeyboard = new WindowsKeyboard();
		uint key = 1;
		uint modifier = 2;

		// act
		// shortcut
		Assert.IsTrue(windowsKeyboard.ProcessKeyBlocking(WindowsKeyboard.WM_KEYDOWN, modifier, key, modifier, scheduler, callback));
		Assert.IsFalse(windowsKeyboard.ProcessKeyBlocking(WindowsKeyboard.WM_KEYDOWN, key, key, modifier, scheduler, callback));

		// hold
		Assert.IsFalse(windowsKeyboard.ProcessKeyBlocking(WindowsKeyboard.WM_KEYDOWN, modifier, key, modifier, scheduler, callback));
		Assert.IsFalse(windowsKeyboard.ProcessKeyBlocking(WindowsKeyboard.WM_KEYDOWN, key, key, modifier, scheduler, callback));

		// release key
		Assert.IsTrue(windowsKeyboard.ProcessKeyBlocking(WindowsKeyboard.WM_KEYUP, key, key, modifier, scheduler, callback));

		// press key
		Assert.IsFalse(windowsKeyboard.ProcessKeyBlocking(WindowsKeyboard.WM_KEYDOWN, key, key, modifier, scheduler, callback));

		// release key
		Assert.IsTrue(windowsKeyboard.ProcessKeyBlocking(WindowsKeyboard.WM_KEYUP, key, key, modifier, scheduler, callback));

		// assert
		scheduler.AdvanceBy(1);
		Assert.AreEqual(2, calledCount);
	}

	[Test]
	public void UnrelatedKeyPresses_ReturnTrue()
	{
		// arrange
		var scheduler = new TestScheduler();
		var calledCount = 0;
		Action callback = void () => calledCount++;
		var windowsKeyboard = new WindowsKeyboard();
		uint key = 1;
		uint modifier = 2;

		// act
		Assert.IsTrue(windowsKeyboard.ProcessKeyBlocking(WindowsKeyboard.WM_KEYDOWN, 435345, key, modifier, scheduler, callback));
		Assert.IsTrue(windowsKeyboard.ProcessKeyBlocking(WindowsKeyboard.WM_KEYDOWN, 734598, key, modifier, scheduler, callback));
		Assert.IsTrue(windowsKeyboard.ProcessKeyBlocking(WindowsKeyboard.WM_KEYDOWN, 359345, key, modifier, scheduler, callback));
		Assert.IsTrue(windowsKeyboard.ProcessKeyBlocking(WindowsKeyboard.WM_KEYDOWN, 378434, key, modifier, scheduler, callback));
		Assert.IsTrue(windowsKeyboard.ProcessKeyBlocking(WindowsKeyboard.WM_KEYDOWN, 421434, key, modifier, scheduler, callback));

		Assert.IsTrue(windowsKeyboard.ProcessKeyBlocking(WindowsKeyboard.WM_KEYUP, 435345, key, modifier, scheduler, callback));
		Assert.IsTrue(windowsKeyboard.ProcessKeyBlocking(WindowsKeyboard.WM_KEYUP, 734598, key, modifier, scheduler, callback));
		Assert.IsTrue(windowsKeyboard.ProcessKeyBlocking(WindowsKeyboard.WM_KEYUP, 359345, key, modifier, scheduler, callback));
		Assert.IsTrue(windowsKeyboard.ProcessKeyBlocking(WindowsKeyboard.WM_KEYUP, 378434, key, modifier, scheduler, callback));
		Assert.IsTrue(windowsKeyboard.ProcessKeyBlocking(WindowsKeyboard.WM_KEYUP, 421434, key, modifier, scheduler, callback));

		// assert
		scheduler.AdvanceBy(1);
		Assert.AreEqual(0, calledCount);
	}

	[Test]
	public void AfterShortcutPressed_AndReleased_ThenUnrelatedKeyPress_ReturnsTrue()
	{
		// arrange
		var scheduler = new TestScheduler();
		var calledCount = 0;
		Action callback = void () => calledCount++;
		var windowsKeyboard = new WindowsKeyboard();
		uint key = 1;
		uint modifier = 2;

		// act
		// shortcut
		Assert.IsTrue(windowsKeyboard.ProcessKeyBlocking(WindowsKeyboard.WM_KEYDOWN, modifier, key, modifier, scheduler, callback));
		Assert.IsFalse(windowsKeyboard.ProcessKeyBlocking(WindowsKeyboard.WM_KEYDOWN, key, key, modifier, scheduler, callback));

		// release
		Assert.IsTrue(windowsKeyboard.ProcessKeyBlocking(WindowsKeyboard.WM_KEYUP, key, key, modifier, scheduler, callback));
		Assert.IsTrue(windowsKeyboard.ProcessKeyBlocking(WindowsKeyboard.WM_KEYUP, modifier, key, modifier, scheduler, callback));

		// unrelated keys
		Assert.IsTrue(windowsKeyboard.ProcessKeyBlocking(WindowsKeyboard.WM_KEYDOWN, 435345, key, modifier, scheduler, callback));
		Assert.IsTrue(windowsKeyboard.ProcessKeyBlocking(WindowsKeyboard.WM_KEYDOWN, 734598, key, modifier, scheduler, callback));
		Assert.IsTrue(windowsKeyboard.ProcessKeyBlocking(WindowsKeyboard.WM_KEYUP, 435345, key, modifier, scheduler, callback));
		Assert.IsTrue(windowsKeyboard.ProcessKeyBlocking(WindowsKeyboard.WM_KEYUP, 734598, key, modifier, scheduler, callback));

		// assert
		scheduler.AdvanceBy(1);
		Assert.AreEqual(1, calledCount);
	}
}