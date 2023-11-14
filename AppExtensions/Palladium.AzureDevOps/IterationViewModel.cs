using AzureDevOpsTools;

namespace Palladium.AzureDevOps;

public class IterationViewModel
{
	public IterationViewModel(Iteration iteration)
	{
		Iteration = iteration;
	}
	
	public Iteration Iteration { get; }

	public string IterationName => Iteration.DisplayName;

	public int StartColumnIndex { get; set; }
	public int RowIndex { get; set; }
	public int EndColumnIndexExclusive { get; set; }
	public int ColumnSpan => EndColumnIndexExclusive - StartColumnIndex;
}