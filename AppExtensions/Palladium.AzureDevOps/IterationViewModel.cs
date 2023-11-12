using AzureDevOpsTools;

namespace Palladium.AzureDevOps;

public class IterationViewModel
{
	public IterationViewModel(Iteration columnChangeIteration)
	{
		IterationName = columnChangeIteration.DisplayName;
	}

	public string IterationName { get; }

	public int StartColumnIndex { get; init; }
	public int RowIndex { get; init; }
	public int EndColumnIndexExclusive { get; set; }
	public int ColumnSpan => EndColumnIndexExclusive - StartColumnIndex;
}