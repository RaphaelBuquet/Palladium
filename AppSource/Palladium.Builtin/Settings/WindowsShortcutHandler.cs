using System.Buffers;
using System.Runtime.InteropServices;

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
	public Task<Shortcut?> TryGetStartupShortcut()
	{
		return Task.Run<Shortcut?>(() =>
		{
			string shortcutPath = ShortcutPath();
			if (!File.Exists(shortcutPath))
			{
				return null;
			}
			char[] buffer = ArrayPool<char>.Shared.Rent(1000);
			try
			{
				int hr = GetShellLinkArguments(shortcutPath, buffer, buffer.Length);
				if (hr < 0) Marshal.ThrowExceptionForHR(hr);
				return new Shortcut { Arguments = buffer.ToString() };
			}
			finally
			{
				ArrayPool<char>.Shared.Return(buffer);
			}
		});
	}

	/// <inheritdoc />
	public Task CreateStartupShortcut(Shortcut shortcut)
	{
		return Task.Run(() =>
		{
			string? exePath = Environment.ProcessPath;
			if (exePath is null || !exePath.EndsWith(".exe") || !File.Exists(exePath))
			{
				throw new Exception("Unable to get the path to the executing application. A startup shortcut can therefore not be created.");
			}
			int hr = CreateShellLink(ShortcutPath(), exePath, shortcut.Arguments);
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
	public static extern int CreateShellLink(string lpszShortcutPath, string lpszFilePath, string? lpszArgs);

	[DllImport("Palladium.NativeWindows.dll", CharSet = CharSet.Unicode)]
	public static extern int GetShellLinkArguments(string lpszShortcutPath, [Out] char[] lpszArgs, int nArgSize);
}