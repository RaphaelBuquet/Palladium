using System.Reflection;
using System.Runtime.InteropServices;
using Palladium.Builtin.SearchOverride;

namespace Palladium.Builtin.Settings;

public class WindowsShortcutHandler : IShortcutHandler
{
	private string StartupFolderPath()
	{
		return Environment.GetFolderPath(Environment.SpecialFolder.Startup);
	}

	private string ShortcutPath()
	{
		return Path.Combine(StartupFolderPath(), "Palladium.lnk");
	}

	/// <inheritdoc />
	public Task<bool> DoesStartupShortcutExist()
	{
		string shortcutPath = ShortcutPath();
		return Task.FromResult(File.Exists(shortcutPath));
	}

	/// <inheritdoc />
	public Task CreateStartupShortcut()
	{
		return Task.Run(() =>
		{
			string? exePath = Environment.ProcessPath;
			if (exePath is null || !exePath.EndsWith(".exe") || !File.Exists(exePath))
			{
				throw new Exception("Unable to get the path to the executing application. A startup shortcut can therefore not be created.");
			}
			int hr = CreateShellLink(ShortcutPath(), exePath);
			if (hr < 0) Marshal.ThrowExceptionForHR(hr);
		});
	}

	/// <inheritdoc />
	public Task RemoveStartupShortcut()
	{
		string shortcutPath = ShortcutPath();
		if (File.Exists(shortcutPath))
		{
			File.Delete(shortcutPath);
		}
		return Task.CompletedTask;
	}

	[DllImport("Palladium.NativeWindows.dll", CharSet = CharSet.Unicode)]
	public static extern int CreateShellLink(string lpszShortcutPath, string lpszFilePath);
}