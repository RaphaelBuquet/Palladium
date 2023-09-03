using System.Reactive.Concurrency;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// ReSharper disable IdentifierTypo

[assembly: InternalsVisibleTo("Palladium.BuiltinActions.Tests")]

namespace Palladium.BuiltinActions.SearchOverride;

public class WindowsKeyboard
{
	public delegate IntPtr KeyboardProc(int nCode, int wParam, IntPtr lParam);

	public const int VK_LWIN = 0x5B;
	public const int VK_S = 0x53;

	private const int WH_KEYBOARD_LL = 13;
	private const int HC_ACTION = 0;
	internal const int WM_KEYDOWN = 0x0100;
	internal const int WM_KEYUP = 0x0101;


	private ShortcutKeyboardState shortcutKeyboardState = new ();

	private IntPtr hookHandle = IntPtr.Zero;

	private KeyboardProc? keyboardCallback;

	[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	private static extern IntPtr SetWindowsHookEx(int idHook, KeyboardProc? lpfn, IntPtr hMod, uint dwThreadId);

	[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool UnhookWindowsHookEx(IntPtr hhk);

	[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

	/// <summary>
	///     Add a keyboard shortcut listener.
	/// </summary>
	/// <param name="callback">Code to run when the keys are pressed down.</param>
	/// <param name="scheduler">Scheduler to send the callback to.</param>
	/// <param name="key"></param>
	/// <param name="modifier"></param>
	public void InstallKeyboardShortcut(Action callback, IScheduler scheduler, uint key, uint modifier)
	{
		IntPtr InternalCallback(int nCode, int wParam, IntPtr lParam)
		{
			var callNext = true;
			if (nCode == HC_ACTION)
			{
				callNext = ProcessKey(wParam, lParam, key, modifier, scheduler, callback);
			}
			if (callNext) return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
			return -1; // prevent the system from passing the message to the rest of the hook chain 
		}

		SetHook(InternalCallback);
	}

	private bool ProcessKey(int keyState, IntPtr lParam, uint key, uint modifier, IScheduler scheduler, Action callback)
	{
		var data = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);

		return ProcessKeyBlocking(keyState, data.vkCode, key, modifier, scheduler, callback);
	}

	/// <summary>
	///     Process a keyboard event. The event may be consumed if it forms part of a recognized
	///     key/modifier combination, in which case it will not be propagated to other handlers.
	/// </summary>
	/// <param name="keyState">New state (up/down)</param>
	/// <param name="eventKeyCode">The code of the key whose state has changed.</param>
	/// <param name="key">The code of the key to press to trigger the shortcut.</param>
	/// <param name="modifier">The code of the modifier key to press to trigger the shortcut.</param>
	/// <param name="scheduler">The scheduler to invoke the callback with.</param>
	/// <param name="callback">Callback when shortcut is pressed.</param>
	/// <returns>True to propagate the event to the rest of the chain, false to block.</returns>
	internal bool ProcessKeyBlocking(int keyState, uint eventKeyCode, uint key, uint modifier, IScheduler scheduler, Action callback)
	{
		// Log.Emit(new EventId(), LogLevel.Debug, $"KEY: WM={keyState:X} vk={eventKeyCode:X}");

		if (keyState == WM_KEYDOWN)
		{
			if (eventKeyCode == modifier)
			{
				// block events when the key is being held down
				if (shortcutKeyboardState.ModifierIsPressed)
				{
					return false;
				}

				shortcutKeyboardState.ModifierIsPressed = true;
			}
			else if (eventKeyCode == key)
			{
				// block events when the key is held down
				if (shortcutKeyboardState.KeyIsPressed)
				{
					return false;
				}

				if (shortcutKeyboardState.ModifierIsPressed)
				{
					shortcutKeyboardState.KeyIsPressed = true;
					scheduler.Schedule(callback);
					return false;
				}
			}
		}
		else if (keyState == WM_KEYUP)
		{
			if (eventKeyCode == key)
			{
				shortcutKeyboardState.KeyIsPressed = false;
			}
			else if (eventKeyCode == modifier)
			{
				shortcutKeyboardState.ModifierIsPressed = false;
			}
		}

		// if (eventKeyCode == VK_LWIN)
		// {
		// 	string type  = keyState == WM_KEYUP ? "up" : "down";
		// 	Log.Emit(new EventId(), LogLevel.Debug, $"SENDING WIN KEY {type}");
		// }

		// For all other cases/key events, propagate to other handlers
		return true;
	}

	public void SetHook(KeyboardProc? proc)
	{
		// Store the callback delegate instance in a field to prevent it from being garbage collected
		keyboardCallback = proc;

		// Set the hook
		hookHandle = SetWindowsHookEx(WH_KEYBOARD_LL, keyboardCallback, IntPtr.Zero, 0);
	}

	public void UnsetHook()
	{
		// Unset the hook
		UnhookWindowsHookEx(hookHandle);
		hookHandle = IntPtr.Zero;
		keyboardCallback = null;

		shortcutKeyboardState = new ShortcutKeyboardState ();
	}

	private struct ShortcutKeyboardState
	{
		public ShortcutKeyboardState()
		{ }

		public bool KeyIsPressed = false;
		public bool ModifierIsPressed = false;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct KBDLLHOOKSTRUCT
	{
		public uint vkCode;
		public uint scanCode;
		public uint flags;
		public uint time;
		public IntPtr dwExtraInfo;
	}
}