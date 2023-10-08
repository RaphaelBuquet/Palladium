namespace Palladium.Builtin.Settings;

public struct Shortcut : IEquatable<Shortcut>
{
	public string? Arguments;

	/// <inheritdoc />
	public bool Equals(Shortcut other)
	{
		return StringComparer.OrdinalIgnoreCase.Equals(Arguments, other.Arguments);
	}

	/// <inheritdoc />
	public override bool Equals(object? obj)
	{
		return obj is Shortcut other && Equals(other);
	}

	/// <inheritdoc />
	public override int GetHashCode()
	{
		return Arguments != null ? StringComparer.OrdinalIgnoreCase.GetHashCode() : 0;
	}

	public static bool operator ==(Shortcut left, Shortcut right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(Shortcut left, Shortcut right)
	{
		return !left.Equals(right);
	}

	/// <inheritdoc />
	public override string ToString()
	{
		return $"{nameof(Arguments)}: {Arguments}";
	}
}