using System.Globalization;

namespace Palladium.Converters.Tests;

public class EnumerableConvertersTests
{
	[Test]
	public void EnumerableContainsItemsToBoolConverter_Empty()
	{
		// arrange
		var c = new EnumerableContainsItemsToBoolConverter();
		object[] empty = Array.Empty<object>();

		// act
		object result = c.Convert(empty, typeof(bool), null, CultureInfo.InvariantCulture);

		// arrange
		Assert.AreEqual(false, result);
	}

	[Test]
	public void EnumerableContainsItemsToBoolConverter_NotEmpty()
	{
		// arrange
		var c = new EnumerableContainsItemsToBoolConverter();
		var notEmpty = new object[] { new () };

		// act
		object result = c.Convert(notEmpty, typeof(bool), null, CultureInfo.InvariantCulture);

		// arrange
		Assert.AreEqual(true, result);
	}

	[Test]
	public void EnumerableContainsNoItemsToBoolConverter_Empty()
	{
		// arrange
		var c = new EnumerableContainsNoItemsToBoolConverter();
		object[] empty = Array.Empty<object>();

		// act
		object result = c.Convert(empty, typeof(bool), null, CultureInfo.InvariantCulture);

		// arrange
		Assert.AreEqual(true, result);
	}

	[Test]
	public void EnumerableContainsNoItemsToBoolConverter_NotEmpty()
	{
		// arrange
		var c = new EnumerableContainsNoItemsToBoolConverter();
		var notEmpty = new object[] { new () };

		// act
		object result = c.Convert(notEmpty, typeof(bool), null, CultureInfo.InvariantCulture);

		// arrange
		Assert.AreEqual(false, result);
	}
}