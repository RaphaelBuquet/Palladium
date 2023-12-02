namespace Palladium.AzureDevOps;

public interface IGridItemViewModel
{
	public int StartColumnIndex { get; }
	public int ColumnSpan { get; }
	public int RowIndex { get; }
}