using System.Xml.Linq;

namespace Palladium.Settings;

public static class XExtensions
{
	public static XElement GetElementOrCreate(this XElement element, XName name)
	{
		var target = element.Element(name);
		if (target != null)
		{
			return target;
		}
		target = new XElement(name);
		element.Add(target);
		return target;
	}

	public static bool TryGetGuidAttribute(this XElement element, out Guid guid)
	{
		guid = Guid.Empty;
		string? guidString = element.Attribute("Guid")?.Value;
		if (guidString is null) return false;
		return Guid.TryParse(guidString, out guid);
	}
}