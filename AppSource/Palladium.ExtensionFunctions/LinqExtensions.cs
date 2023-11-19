namespace Palladium.ExtensionFunctions;

public static class LinqExtensions
{
	public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> enumerable)
	{
		return enumerable.Where(x => x != null)!;
	}
}