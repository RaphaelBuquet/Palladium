using Microsoft.Extensions.Configuration;

namespace Common.Core.Extensions;

public static class ConfigurationExtension
{
	public static IConfigurationBuilder Initialize(this IConfigurationBuilder builder)
	{
		string env = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";

		return builder
			.SetBasePath(Directory.GetCurrentDirectory())
			.AddJsonFile("appsettings.json", true, true)
			.AddJsonFile($"appsettings.{env}.json", true, true)
			.AddEnvironmentVariables();
	}
}