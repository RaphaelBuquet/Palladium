using System.Reactive.Concurrency;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Palladium.Logging;

// ReSharper disable IdentifierTypo

namespace Palladium.BuiltinActions.SearchOverride;

public static class WindowsKeyboard
{
	public delegate IntPtr KeyboardProc(int nCode, int wParam, IntPtr lParam);

	public const int VK_LWIN = 0x5B;
	public const int VK_S = 0x53;

	private const int WH_KEYBOARD_LL = 13;
	private const int HC_ACTION = 0;
	private const int WM_KEYDOWN = 0x0100;
	private const int WM_KEYUP = 0x0101;
	private static readonly Dictionary<uint, int> keyStates = new ();

	private static IntPtr hookHandle = IntPtr.Zero;

	private static KeyboardProc? callback;

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
	/// <param name="keyOne"></param>
	/// <param name="keyTwo"></param>
	/// <param name="keyThree"></param>
	public static void InstallKeyboardShortcut(Action callback, IScheduler scheduler, uint keyOne, uint? keyTwo, uint? keyThree)
	{
		bool ProcessKey(int wParam, IntPtr lParam)
		{
			var data = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);

			// update key states
			if (data.vkCode == keyOne || data.vkCode == keyTwo || data.vkCode == keyThree)
			{
				keyStates.TryGetValue(data.vkCode, out int previousValue);
				keyStates[data.vkCode] = wParam;

				// only proceed if the value has changed, we don't want to constantly raise the callback when the keys are held.
				if (previousValue == wParam)
				{
					return true;
				}
			}

			Log.Emit(new EventId(), LogLevel.Debug, $"KEY: WM={wParam:X} vk={data.vkCode:X}");

			// check all required keys are down
			if (wParam == WM_KEYDOWN && keyStates.TryGetValue(keyOne, out int keyOneState))
			{
				bool keyOneValid = keyOneState == WM_KEYDOWN;
				var keyTwoValid = true;
				var keyThreeValid = true;
				if (keyTwo.HasValue)
				{
					keyTwoValid = keyStates.TryGetValue(keyTwo.Value, out int keyTwoState) && keyTwoState == WM_KEYDOWN;
				}
				if (keyThree.HasValue)
				{
					keyThreeValid = keyStates.TryGetValue(keyThree.Value, out int keyThreeState) && keyThreeState == WM_KEYDOWN;
				}

				if (keyOneValid && keyTwoValid && keyThreeValid)
				{
					scheduler.Schedule(callback);
					return false;
				}
			}

			return true;
		}

		IntPtr InternalCallback(int nCode, int wParam, IntPtr lParam)
		{
			var callNext = true;
			if (nCode == HC_ACTION)
			{
				callNext = ProcessKey(wParam, lParam);
			}
			if (callNext) return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
			return -1; // prevent the system from passing the message to the rest of the hook chain 
		}

		SetHook(InternalCallback);
	}

	public static void SetHook(KeyboardProc? proc)
	{
		// Store the callback delegate instance in a field to prevent it from being garbage collected
		callback = proc;

		// Set the hook
		hookHandle = SetWindowsHookEx(WH_KEYBOARD_LL, callback, IntPtr.Zero, 0);
	}

	public static void UnsetHook()
	{
		// Unset the hook
		UnhookWindowsHookEx(hookHandle);
		hookHandle = IntPtr.Zero;
		callback = null;
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