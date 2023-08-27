using System.Reflection;
using Microsoft.Extensions.Logging;
using Palladium.ActionsService;
using Palladium.Logging;

namespace Palladium.Extensions;

public class ExtensionsLoader
{
	private readonly string extensionsDirectory;

	public ExtensionsLoader(string extensionsDirectory)
	{
		this.extensionsDirectory = extensionsDirectory;
	}

	public void LoadExtensions(ActionsRepositoryService actionsRepositoryService)
	{
		var extensionsToInvoke = new List<Type>();

		if (!Path.Exists(extensionsDirectory))
		{
			Log.Emit(new EventId(), LogLevel.Warning, $"Could not load extensions as \"{extensionsDirectory}\" does not exist.");
			return;
		}

		foreach (string extensionDirectory in Directory.EnumerateDirectories(extensionsDirectory, "*", SearchOption.TopDirectoryOnly))
		{
			string assemblyName = Path.GetFileName(extensionDirectory);

			string assemblyFilePath = Path.Combine(extensionDirectory, $"{assemblyName}.dll");
			if (!File.Exists(assemblyFilePath))
			{
				Log.Emit(new EventId(), LogLevel.Error, $"The extension \"{assemblyName}\" at \"{extensionDirectory}\" does not have a corresponding assembly at \"{assemblyFilePath}\".");
				continue;
			}

			var loadContext = new PluginLoadContext(assemblyFilePath);
			Assembly? assembly = null;
			try
			{
				assembly = loadContext.LoadFromAssemblyName(new AssemblyName(assemblyName));
			}
			catch (Exception e)
			{
				Log.Emit(new EventId(), LogLevel.Error, $"Failed to load extension at \"{extensionDirectory}\"", e);
			}
			if (assembly != null)
			{
				extensionsToInvoke.AddRange(assembly.ExportedTypes.Where(t => t.BaseType == typeof(ExtensionBase)));
			}
		}

		foreach (Type extensionType in extensionsToInvoke)
		{
			try
			{
				if (Activator.CreateInstance(extensionType) is ExtensionBase loadedExtension)
				{
					loadedExtension.ActionsRepositoryService = actionsRepositoryService;
					loadedExtension.Init();
				}
			}
			catch (Exception e)
			{
				Log.Emit(new EventId(), LogLevel.Error, $"Failed to initialize \"{extensionType.AssemblyQualifiedName}\".", e);
			}
		}
	}
}