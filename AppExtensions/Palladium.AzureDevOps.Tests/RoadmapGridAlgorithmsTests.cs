using Avalonia.Controls;
using AzureDevOpsTools;

namespace Palladium.AzureDevOps.Tests;

public class RoadmapGridAlgorithmsTests
{
	[Test]
	public void CreateIterationsGrid_Empty()
	{
		// ReSharper disable once CollectionNeverUpdated.Local
		var iterations = new List<Iteration>();
		var result = RoadmapGridAlgorithms.CreateIterationsGrid(iterations);
		Assert.AreEqual(0, result.IterationViewModels.Count);
		Assert.AreEqual(0, result.Columns.Count);
		Assert.AreEqual(0, result.Rows.Count);
	}

	[Test]
	public void CreateIterationsGrid_SingleIteration()
	{
		var iterations = new List<Iteration>()
		{
			new ()
			{
				DisplayName = "M1",
				StartDate = new DateTime(2023, 11, 1),
				EndDate = new DateTime(2023, 11, 30),
				IterationPath = "Palladium\\M1"
			}
		};
		var result = RoadmapGridAlgorithms.CreateIterationsGrid(iterations);
		Assert.AreEqual(1, result.IterationViewModels.Count);
		
		Assert.AreEqual("M1", result.IterationViewModels[0].IterationName);
		Assert.AreEqual(RoadmapGridAlgorithms.IterationRowOffset, result.IterationViewModels[0].RowIndex);
		Assert.AreEqual(RoadmapGridAlgorithms.IterationColumnOffset, result.IterationViewModels[0].StartColumnIndex);
		Assert.AreEqual(RoadmapGridAlgorithms.IterationColumnOffset + 1, result.IterationViewModels[0].EndColumnIndexExclusive);
		Assert.AreEqual(1, result.Columns.Count);
		Assert.AreEqual(new GridLength(29, GridUnitType.Star), result.Columns[0]);
		Assert.AreEqual(1, result.Rows.Count);
		Assert.AreEqual(GridLength.Auto, result.Rows[0]);
	}
	
	[Test]
	public void CreateIterationsGrid_TwoIterations()
	{
		var iterations = new List<Iteration>()
		{
			new ()
			{
				DisplayName = "M1",
				StartDate = new DateTime(2023, 11, 1),
				EndDate = new DateTime(2023, 11, 30),
				IterationPath = "Palladium\\M1"
			},
			new ()
			{
				DisplayName = "M2",
				StartDate = new DateTime(2023, 12, 1),
				EndDate = new DateTime(2023, 12, 31),
				IterationPath = "Palladium\\M2"
			}
		};
		var result = RoadmapGridAlgorithms.CreateIterationsGrid(iterations);
		Assert.AreEqual(2, result.IterationViewModels.Count);
		Assert.AreEqual(3, result.Columns.Count);
		Assert.AreEqual(1, result.Rows.Count);
		
		Assert.AreEqual("M1", result.IterationViewModels[0].IterationName);
		Assert.AreEqual(RoadmapGridAlgorithms.IterationRowOffset, result.IterationViewModels[0].RowIndex);
		Assert.AreEqual(RoadmapGridAlgorithms.IterationColumnOffset, result.IterationViewModels[0].StartColumnIndex);
		Assert.AreEqual(RoadmapGridAlgorithms.IterationColumnOffset + 1, result.IterationViewModels[0].EndColumnIndexExclusive);
		
		Assert.AreEqual("M2", result.IterationViewModels[1].IterationName);
		Assert.AreEqual(RoadmapGridAlgorithms.IterationRowOffset, result.IterationViewModels[1].RowIndex);
		Assert.AreEqual(RoadmapGridAlgorithms.IterationColumnOffset + 2, result.IterationViewModels[1].StartColumnIndex);
		Assert.AreEqual(RoadmapGridAlgorithms.IterationColumnOffset + 3, result.IterationViewModels[1].EndColumnIndexExclusive);
	}
}