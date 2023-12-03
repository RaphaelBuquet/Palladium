namespace Palladium.AzureDevOps;

internal static class Maths
{
	// AI generated
	public static double InverseLerp(long a, long b, long value)
	{
		// Ensure a is smaller than b
		if (a > b)
		{
			(a, b) = (b, a);
		}

		// Ensure value is within the bounds [a, b]
		if (value < a)
			return 0.0;
		else if (value > b)
			return 1.0;

		// Calculate the ratio
		double ratio = (double)(value - a) / (b - a);

		return ratio;
	}
}