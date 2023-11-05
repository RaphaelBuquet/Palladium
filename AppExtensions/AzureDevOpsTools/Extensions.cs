namespace AzureDevOpsTools;

public static class Extensions
{
	public static TValue TryGetWithDefault<TKey, TValue>(this IDictionary<TKey, object> dictionary, TKey key, TValue defaultValue)
	{
		if (!dictionary.TryGetValue(key, out object? value))
		{
			return defaultValue;
		}
		if (value is TValue convertedValue)
		{
			return convertedValue;
		}
		else
		{
			return defaultValue;
		}
	}
}