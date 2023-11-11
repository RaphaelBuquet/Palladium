namespace Palladium.AzureDevOps;

public record RoadmapSettings
{
	public string? OrganisationUrl { get; init; }
	public string? ConnectionTokenEncrypted { get; init; }
}