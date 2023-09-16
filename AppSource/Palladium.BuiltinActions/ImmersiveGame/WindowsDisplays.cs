using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Palladium.Logging;

namespace Palladium.BuiltinActions.ImmersiveGame;

// NOTE: this code was partially generated with JetBrains AI assistant
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class WindowsDisplays : IDisplaySource
{
	private readonly Log log;

	public WindowsDisplays(Log log)
	{
		this.log = log;
	}

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
	MiniLog IDisplaySource.DisableNonPrimaryDisplays()
	{
		int result = DisableNonPrimaryDisplays();
		log.Emit(new EventId(), LogLevel.Debug, $"{nameof(DisableNonPrimaryDisplays)} returned {result}");
		
		return InterpretResult(result);
	}
	
	/// <inheritdoc />
	MiniLog IDisplaySource.RestoreSettings()
	{
		int result = RestoreSettings();
		log.Emit(new EventId(), LogLevel.Debug, $"{nameof(RestoreSettings)} returned {result}");
		
		return InterpretResult(result);
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
	
	private static MiniLog InterpretResult(int result)
	{
		var miniLogProvider = new MiniLogProvider();
		bool success = result == 0;
		if (!success)
		{
			miniLogProvider.Entries.OnNext(new MiniLog.Entry
			{
				LogLevel = LogLevel.Error,
				Message = $"Operation failed with code {result}"
			});
		}
		miniLogProvider.Result.OnNext(success);
		return miniLogProvider.Value;
	}
}