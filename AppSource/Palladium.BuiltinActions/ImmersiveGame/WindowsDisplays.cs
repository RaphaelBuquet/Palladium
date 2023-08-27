﻿using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Palladium.Logging;

namespace Palladium.BuiltinActions.ImmersiveGame;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public class WindowsDisplays : IDisplaySource
{
	[Flags]
	public enum DisplayDeviceStateFlags
	{
		DISPLAY_DEVICE_ACTIVE = 0x00000001,
		DISPLAY_DEVICE_MIRRORING_DRIVER = 0x00000008,
		DISPLAY_DEVICE_MODESPRUNED = 0x8000000,
		DISPLAY_DEVICE_PRIMARY_DEVICE = 0x00000004,
		DISPLAY_DEVICE_REMOVABLE = 0x00000020,
		DISPLAY_DEVICE_VGA_COMPATIBLE = 0x00000010
	}

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern bool EnumDisplayDevices(string? lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

	[DllImport("Palladium.NativeWindows.dll")]
	public static extern int DisableNonPrimaryDisplays();

	[DllImport("Palladium.NativeWindows.dll")]
	public static extern int RestoreSettings();

	public static (DISPLAY_DEVICE[] adapters, DISPLAY_DEVICE[] displays) GetDisplayDevices()
	{
		var adapters = new List<DISPLAY_DEVICE>();
		var displays = new List<DISPLAY_DEVICE>();
		var device = new DISPLAY_DEVICE();
		device.cb = Marshal.SizeOf(device);

		// Enumerate adapters
		uint adapterIndex = 0;
		while (EnumDisplayDevices(null, adapterIndex, ref device, 0))
		{
			adapters.Add(device);

			// Enumerate displays for each adapter
			uint displayIndex = 0;
			while (EnumDisplayDevices(device.DeviceName, displayIndex, ref device, 0))
			{
				displays.Add(device);
				displayIndex++;
			}

			adapterIndex++;
		}

		return (adapters.ToArray(), displays.ToArray());
	}

	/// <inheritdoc />
	bool IDisplaySource.DisableNonPrimaryDisplays()
	{
		int result = DisableNonPrimaryDisplays();
		Log.Emit(new EventId(), LogLevel.Debug, $"{nameof(DisableNonPrimaryDisplays)} returned {result}");
		return result == 0;
	}

	/// <inheritdoc />
	bool IDisplaySource.RestoreSettings()
	{
		int result = RestoreSettings();
		Log.Emit(new EventId(), LogLevel.Debug, $"{nameof(RestoreSettings)} returned {result}");
		return result == 0;
	}

	/// <inheritdoc />
	Task<string[]> IDisplaySource.GetDisplayDevices()
	{
		return Task.Run(() => GetDisplayDevices().displays.Select(d => d.DeviceName).ToArray());
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	public struct DISPLAY_DEVICE
	{
		public int cb;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
		public string DeviceName;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
		public string DeviceString;

		public DisplayDeviceStateFlags StateFlags;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
		public string DeviceID;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
		public string DeviceKey;
	}
}