namespace AzureDevOpsTools;

public struct WorkItemState : IEquatable<WorkItemState>
{
	public required string WorkItemType;
	public required string State;

	/// <inheritdoc />
	public bool Equals(WorkItemState other)
	{
		return WorkItemType == other.WorkItemType && State == other.State;
	}

	/// <inheritdoc />
	public override bool Equals(object? obj)
	{
		return obj is WorkItemState other && Equals(other);
	}

	/// <inheritdoc />
	public override int GetHashCode()
	{
		return HashCode.Combine(WorkItemType, State);
	}

	public static bool operator ==(WorkItemState left, WorkItemState right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(WorkItemState left, WorkItemState right)
	{
		return !left.Equals(right);
	}
}