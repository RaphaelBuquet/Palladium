﻿namespace Palladium.AzureDevOps;

public record RoadmapSettings
{
	public string? OrganisationUrl { get; init; }
	public string? ConnectionTokenEncrypted { get; init; }
	public string? ProjectId { get; init; }
	public string? PlanId { get; init; }
	public string? QueryId { get; init; }

	public string? ConnectionTokenDecrypted => RoadmapSettingsViewModel.Decrypt(ConnectionTokenEncrypted);
}