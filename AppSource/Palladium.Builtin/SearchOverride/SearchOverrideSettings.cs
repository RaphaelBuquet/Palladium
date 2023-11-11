namespace Palladium.Builtin.SearchOverride;

public record SearchOverrideSettings
{
	public string? BrowserPath { get; init; }
	public string? BrowserArguments { get; init; }
	public bool EnableOnAppStart { get; init; }
}