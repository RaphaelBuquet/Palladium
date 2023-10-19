using System.Reactive.Concurrency;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// ReSharper disable IdentifierTypo

[assembly: InternalsVisibleTo("Palladium.Builtin.Tests")]

namespace Palladium.Builtin.SearchOverride;

public class WindowsKeyboard
{
	public delegate IntPtr KeyboardProc(int nCode, int wParam, IntPtr lParam);

	// vk codes of modifiers
	public const int VK_LWIN = 0x5B;
	public const int VK_RWIN = 0x5C;
	public const int VK_LSHIFT = 0xA0;
	public const int VK_RSHIFT = 0xA1;
	public const int VK_LCONTROL = 0xA2;
	public const int VK_RCONTROL = 0xA3;
	public const int VK_LMENU = 0xA4;
	public const int VK_RMENU = 0xA5;

	// vk codes of keys
	public const int VK_S = 0x53;

	private const int WH_KEYBOARD_LL = 13;
	private const int HC_ACTION = 0;
	internal const int WM_KEYDOWN = 0x0100;
	internal const int WM_KEYUP = 0x0101;

	private const int INPUT_KEYBOARD = 1;
	private const uint KEYEVENTF_KEYUP = 0x0002;

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

	[DllImport("user32.dll")]
	private static extern short GetKeyState(int nVirtKey);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern uint SendInput(uint cInputs, [ MarshalAs(UnmanagedType.LPArray)] [ In] INPUT[] pInputs, int cbSize);

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

		ProcessedKey result = ProcessKey(keyState, data.vkCode, key, modifier, scheduler, callback);
		if (result.SimulateKeypress)
		{
			var inputDown = new INPUT();
			inputDown.type = INPUT_KEYBOARD;
			inputDown.U.ki.wVk = 0xFF;
			var inputUp = new INPUT();
			inputUp.type = INPUT_KEYBOARD;
			inputUp.U.ki.wVk = 0xFF;
			inputUp.U.ki.dwFlags = KEYEVENTF_KEYUP;
			SendInput(2, new [] { inputDown, inputUp }, Marshal.SizeOf(typeof(INPUT)));
		}
		return result.PropagateEvent;
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
	/// <returns>.</returns>
	internal ProcessedKey ProcessKey(int keyState, uint eventKeyCode, uint key, uint modifier, IScheduler scheduler, Action callback)
	{
		ProcessedKey result = DecideAction(keyState, eventKeyCode, key, modifier, scheduler, callback);
		UpdateState(keyState, eventKeyCode, key, modifier);
		return result;
	}

	private void UpdateState(int keyState, uint eventKeyCode, uint key, uint modifier)
	{
		if (keyState == WM_KEYDOWN)
		{
			if (eventKeyCode == key)
			{
				if (shortcutKeyboardState.IsSingleModifierPressed(modifier))
				{
					shortcutKeyboardState.KeyIsPressed = true;
				}
			}
			else
			{
				shortcutKeyboardState.SetModifierPressed(eventKeyCode);
			}
		}
		else if (keyState == WM_KEYUP)
		{
			if (eventKeyCode == key)
			{
				shortcutKeyboardState.KeyIsPressed = false;
			}
			else
			{
				shortcutKeyboardState.SetModifierUnpressed(eventKeyCode);
			}
		}
	}

	private ProcessedKey DecideAction(int keyState, uint eventKeyCode, uint key, uint modifier, IScheduler scheduler, Action callback)
	{
		if (keyState == WM_KEYDOWN)
		{
			if (eventKeyCode == modifier)
			{
				// block events when the key is being held down
				if (shortcutKeyboardState.IsSingleModifierPressed(modifier))
				{
					return ProcessedKey.Block;
				}
			}
			else if (eventKeyCode == key)
			{
				// block events when the key is held down
				if (shortcutKeyboardState.KeyIsPressed)
				{
					return ProcessedKey.Block;
				}

				if (shortcutKeyboardState.IsSingleModifierPressed(modifier))
				{
					scheduler.Schedule(callback);
					return ProcessedKey.Block;
				}
			}
		}
		else if (keyState == WM_KEYUP)
		{
			if (eventKeyCode == modifier)
			{
				// send keypress so that modifier is eaten up to block things like windows start menu from appearing
				// https://www.autohotkey.com/docs/v1/lib/_MenuMaskKey.htm
				if (shortcutKeyboardState.KeyIsPressed)
				{
					return new ProcessedKey
					{
						PropagateEvent = true,
						SimulateKeypress = true
					};
				}
			}
		}

		// For all other cases/key events, propagate to other handlers
		return ProcessedKey.Propagate;
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

	public static bool IsKeyDown(int vKey)
	{
		return (GetKeyState(vKey) & 0x8000) == 0x8000;
	}

	public struct ProcessedKey
	{
		/// <summary>
		///     True to propagate the event to the rest of the chain, false to block.
		/// </summary>
		public bool PropagateEvent;

		/// <summary>
		///     True to simulate a reserved/non-assigned key press after processing.
		/// </summary>
		public bool SimulateKeypress;

		public static readonly ProcessedKey Propagate = new()  { PropagateEvent = true, SimulateKeypress = false };
		public static readonly ProcessedKey Block = new()  { PropagateEvent = false, SimulateKeypress = false };
	}

	private struct ShortcutKeyboardState
	{
		public ShortcutKeyboardState()
		{ }

		public bool KeyIsPressed = false;
		public Modifiers Modifiers = Modifiers.None;

		public bool IsSingleModifierPressed(uint keyCode)
		{
			switch (keyCode)
			{
				case VK_LWIN:
				case VK_RWIN:
					return Modifiers == Modifiers.Windows;
				case VK_LSHIFT:
				case VK_RSHIFT:
					return Modifiers == Modifiers.Shift;
				case VK_LCONTROL:
				case VK_RCONTROL:
					return Modifiers == Modifiers.Control;
				case VK_LMENU:
				case VK_RMENU:
					return Modifiers == Modifiers.Alt;
				default:
					return false;
			}
		}

		public void SetModifierPressed(uint keyCode)
		{
			switch (keyCode)
			{
				case VK_LWIN:
				case VK_RWIN:
					Modifiers |= Modifiers.Windows;
					break;
				case VK_LSHIFT:
				case VK_RSHIFT:
					Modifiers |= Modifiers.Shift;
					break;
				case VK_LCONTROL:
				case VK_RCONTROL:
					Modifiers |= Modifiers.Control;
					break;
				case VK_LMENU:
				case VK_RMENU:
					Modifiers |= Modifiers.Alt;
					break;
			}
		}

		public void SetModifierUnpressed(uint keyCode)
		{
			switch (keyCode)
			{
				case VK_LWIN:
				case VK_RWIN:
					Modifiers &= ~Modifiers.Windows;
					break;
				case VK_LSHIFT:
				case VK_RSHIFT:
					Modifiers &= ~Modifiers.Shift;
					break;
				case VK_LCONTROL:
				case VK_RCONTROL:
					Modifiers &= ~Modifiers.Control;
					break;
				case VK_LMENU:
				case VK_RMENU:
					Modifiers &= ~Modifiers.Alt;
					break;
			}
		}
	}

	[Flags]
	private enum Modifiers : byte
	{
		None = 0,
		Windows = 1 << 0,
		Shift = 1 << 1,
		Control = 1 << 2,
		Alt = 1 << 3
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

	public struct INPUT
	{
		public int type;
		public InputUnion U;
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct InputUnion
	{
		[FieldOffset(0)]
		public MOUSEINPUT mi;

		[FieldOffset(0)]
		public KEYBDINPUT ki;

		[FieldOffset(0)]
		public HARDWAREINPUT hi;
	}

	public struct MOUSEINPUT
	{
		public int dx;
		public int dy;
		public uint mouseData;
		public uint dwFlags;
		public uint time;
		public IntPtr dwExtraInfo;
	}

	public struct KEYBDINPUT
	{
		public ushort wVk;
		public ushort wScan;
		public uint dwFlags;
		public uint time;
		public IntPtr dwExtraInfo;
	}

	public struct HARDWAREINPUT
	{
		public uint uMsg;
		public ushort wParamL;
		public ushort wParamH;
	}
}