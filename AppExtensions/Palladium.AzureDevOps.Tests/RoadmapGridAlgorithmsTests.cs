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
		RoadmapGridAlgorithms.IterationsGrid result = RoadmapGridAlgorithms.CreateIterationsGrid(iterations);
		Assert.AreEqual(0, result.IterationViewModels.Count);
		Assert.AreEqual(0, result.Columns.Count);
		Assert.AreEqual(0, result.Rows.Count);
	}

	[Test]
	public void CreateIterationsGrid_SingleIteration()
	{
		var iterations = new List<Iteration>()
		{
			new Iteration
			{
				DisplayName = "M1",
				StartDate = new DateTime(2023, 11, 1),
				EndDate = new DateTime(2023, 11, 30),
				IterationPath = "Palladium\\M1"
			}
		};
		RoadmapGridAlgorithms.IterationsGrid result = RoadmapGridAlgorithms.CreateIterationsGrid(iterations);
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
			new Iteration
			{
				DisplayName = "M1",
				StartDate = new DateTime(2023, 11, 1),
				EndDate = new DateTime(2023, 11, 30),
				IterationPath = "Palladium\\M1"
			},
			new Iteration
			{
				DisplayName = "M2",
				StartDate = new DateTime(2023, 12, 1),
				EndDate = new DateTime(2023, 12, 31),
				IterationPath = "Palladium\\M2"
			}
		};
		RoadmapGridAlgorithms.IterationsGrid result = RoadmapGridAlgorithms.CreateIterationsGrid(iterations);
		Assert.AreEqual(2, result.IterationViewModels.Count);
		Assert.AreEqual(3, result.Columns.Count);
		Assert.AreEqual(1, result.Rows.Count);
		Assert.IsTrue(RoadmapGridAlgorithms.HasNoOverlaps(result.IterationViewModels));

		Assert.AreEqual("M1", result.IterationViewModels[0].IterationName);
		Assert.AreEqual(RoadmapGridAlgorithms.IterationRowOffset, result.IterationViewModels[0].RowIndex);
		Assert.AreEqual(RoadmapGridAlgorithms.IterationColumnOffset, result.IterationViewModels[0].StartColumnIndex);
		Assert.AreEqual(RoadmapGridAlgorithms.IterationColumnOffset + 1, result.IterationViewModels[0].EndColumnIndexExclusive);

		Assert.AreEqual("M2", result.IterationViewModels[1].IterationName);
		Assert.AreEqual(RoadmapGridAlgorithms.IterationRowOffset, result.IterationViewModels[1].RowIndex);
		Assert.AreEqual(RoadmapGridAlgorithms.IterationColumnOffset + 2, result.IterationViewModels[1].StartColumnIndex);
		Assert.AreEqual(RoadmapGridAlgorithms.IterationColumnOffset + 3, result.IterationViewModels[1].EndColumnIndexExclusive);
	}

	[Test]
	public void CreateIterationsGrid_OverlappingIterations()
	{
		// arrange
        var iterations = new List<Iteration>()
        {
        	new Iteration
        	{
        		DisplayName = "Sprint",
        		StartDate = new DateTime(2023, 11, 1),
        		EndDate = new DateTime(2023, 11, 30),
        		IterationPath = "Palladium\\Milestone\\Sprint"
        	},
        	new Iteration
        	{
        		DisplayName = "Milestone",
        		StartDate = new DateTime(2023, 10, 1),
        		EndDate = new DateTime(2023, 12, 31),
        		IterationPath = "Palladium\\Milestone"
        	},
        };
        RoadmapGridAlgorithms.IterationsGrid result = RoadmapGridAlgorithms.CreateIterationsGrid(iterations);
        Assert.AreEqual(2, result.IterationViewModels.Count);
        Assert.AreEqual(3, result.Columns.Count);
        Assert.AreEqual(2, result.Rows.Count);
        Assert.IsTrue(RoadmapGridAlgorithms.HasNoOverlaps(result.IterationViewModels));

        var sprintIteration = result.IterationViewModels.Single(x => x.IterationName == "Sprint");
        Assert.AreEqual(RoadmapGridAlgorithms.IterationRowOffset + 1, sprintIteration.RowIndex); // lower iteration is pushed down
        Assert.AreEqual(RoadmapGridAlgorithms.IterationColumnOffset + 1, sprintIteration.StartColumnIndex); 
        Assert.AreEqual(RoadmapGridAlgorithms.IterationColumnOffset + 2, sprintIteration.EndColumnIndexExclusive);

        var milestoneIteration = result.IterationViewModels.Single(x => x.IterationName == "Milestone");
        Assert.AreEqual(RoadmapGridAlgorithms.IterationRowOffset, milestoneIteration.RowIndex);
        Assert.AreEqual(RoadmapGridAlgorithms.IterationColumnOffset, milestoneIteration.StartColumnIndex);
        Assert.AreEqual(RoadmapGridAlgorithms.IterationColumnOffset + 3, milestoneIteration.EndColumnIndexExclusive);
	}
	
	[Test]
	public void CreateIterationsGrid_OverlappingIterations_WithSameIterationHierarchicalLevel()
	{
		// arrange
        var iterations = new List<Iteration>()
        {
        	new Iteration
        	{
        		DisplayName = "Example1",
        		StartDate = new DateTime(2023, 11, 1),
        		EndDate = new DateTime(2023, 11, 30),
        		IterationPath = "Palladium\\Example1"
        	},
        	new Iteration
        	{
        		DisplayName = "Example2",
        		StartDate = new DateTime(2023, 11, 15),
        		EndDate = new DateTime(2023, 12, 15),
        		IterationPath = "Palladium\\Example2"
        	},
        };
        RoadmapGridAlgorithms.IterationsGrid result = RoadmapGridAlgorithms.CreateIterationsGrid(iterations);
        Assert.AreEqual(2, result.IterationViewModels.Count);
        Assert.AreEqual(3, result.Columns.Count);
        Assert.AreEqual(2, result.Rows.Count);
        Assert.IsTrue(RoadmapGridAlgorithms.HasNoOverlaps(result.IterationViewModels));

        var iteration1 = result.IterationViewModels.Single(x => x.IterationName == "Example1");
        Assert.AreEqual(RoadmapGridAlgorithms.IterationRowOffset, iteration1.RowIndex);
        Assert.AreEqual(RoadmapGridAlgorithms.IterationColumnOffset, iteration1.StartColumnIndex); 
        Assert.AreEqual(RoadmapGridAlgorithms.IterationColumnOffset + 2, iteration1.EndColumnIndexExclusive);

        var iteration2 = result.IterationViewModels.Single(x => x.IterationName == "Example2");
        Assert.AreEqual(RoadmapGridAlgorithms.IterationRowOffset + 1, iteration2.RowIndex);
        Assert.AreEqual(RoadmapGridAlgorithms.IterationColumnOffset + 1, iteration2.StartColumnIndex);
        Assert.AreEqual(RoadmapGridAlgorithms.IterationColumnOffset + 3, iteration2.EndColumnIndexExclusive);
	}

	[Test]
	public void CreateWorkItemsGrid_Empty()
	{
		RoadmapGridAlgorithms.WorkItemGrid result = RoadmapGridAlgorithms.CreateWorkItemsGrid(0, Array.Empty<IterationViewModel>(), Array.Empty<RoadmapWorkItem>());
		Assert.AreEqual(0, result.WorkItemViewModels.Count);
		Assert.AreEqual(0, result.Rows.Count);
	}

	[Test]
	public void CreateWorkItemsGrid_SingleIteration()
	{
		// arrange
		var iterations = new List<IterationViewModel>()
		{
			new IterationViewModel (new Iteration
			{
				DisplayName = "M1",
				StartDate = new DateTime(2023, 11, 1),
				EndDate = new DateTime(2023, 11, 30),
				IterationPath = "Palladium\\M1"
			})
			{
				StartColumnIndex = 0,
				EndColumnIndexExclusive = 1,
				RowIndex = 0
			}
		};
		const int workItemsCount = 65;
		var workItems = GenerateWorkItems(iterations[0].Iteration, workItemsCount);

		// act
		RoadmapGridAlgorithms.WorkItemGrid result = RoadmapGridAlgorithms.CreateWorkItemsGrid(0, iterations, workItems);

		// assert
		Assert.AreEqual(workItemsCount, result.Rows.Count);
		Assert.AreEqual(GridLength.Auto, result.Rows[0]);
		WorkItemViewModel firstItem = result.WorkItemViewModels[0];
		WorkItemViewModel lastItem = result.WorkItemViewModels[^1];
		Assert.AreEqual(0, firstItem.RowIndex);
		Assert.AreEqual(workItemsCount - 1, lastItem.RowIndex);
		Assert.AreEqual(0, firstItem.StartColumnIndex);
		Assert.AreEqual(0, lastItem.StartColumnIndex);
		Assert.AreEqual(1, firstItem.EndColumnIndexExclusive);
		Assert.AreEqual(1, lastItem.EndColumnIndexExclusive);
	}

	[Test]
	public void CreateWorkItemsGrid_SingleGridGapsForIterations()
	{
		// arrange
		var iterations = new List<IterationViewModel>()
		{
			new IterationViewModel (new Iteration
			{
				DisplayName = "M1",
				StartDate = new DateTime(2023, 11, 1),
				EndDate = new DateTime(2023, 11, 30),
				IterationPath = "Palladium\\M1"
			})
			{
				StartColumnIndex = 1, // the "gap" is simulated because it starts at 1 instead of 0.
				EndColumnIndexExclusive = 2,
			}
		};
		const int workItemsCount = 1;
		var workItems = GenerateWorkItems(iterations[0].Iteration, workItemsCount);

		// act
		RoadmapGridAlgorithms.WorkItemGrid result = RoadmapGridAlgorithms.CreateWorkItemsGrid(0, iterations, workItems);

		// assert
		WorkItemViewModel firstItem = result.WorkItemViewModels[0];
		Assert.AreEqual(0, firstItem.RowIndex);
		Assert.AreEqual(1, firstItem.StartColumnIndex);
		Assert.AreEqual(2, firstItem.EndColumnIndexExclusive);
	}
	
	[Test]
	public void CreateWorkItemsGrid_WithOverlappingIterations_LongerItemsArePlacedFirst()
	{
		// arrange
		var iterations = new List<IterationViewModel>()
		{
			new IterationViewModel (new Iteration
			{
				DisplayName = "Sprint",
				StartDate = new DateTime(2023, 11, 1),
				EndDate = new DateTime(2023, 11, 30),
				IterationPath = "Palladium\\Milestone\\Sprint"
			})
			{
				StartColumnIndex = 1,
				EndColumnIndexExclusive = 2,
			},
			new IterationViewModel (new Iteration
			{
				DisplayName = "Milestone",
				StartDate = new DateTime(2023, 10, 1),
				EndDate = new DateTime(2023, 12, 31),
				IterationPath = "Palladium\\Milestone"
			})
			{
				StartColumnIndex = 0,
				EndColumnIndexExclusive = 3,
			}
		};
		var workItems = new List<RoadmapWorkItem>
		{
			new RoadmapWorkItem
			{
				Iteration = iterations[0].Iteration,
				State = "Test",
				Title = "Short",
				Type = "Test",
				AssignedTo = null
			},
			// make the long item number two to make sure the algorithm sorts the items
			new RoadmapWorkItem
			{
				Iteration = iterations[1].Iteration,
				State = "Test",
				Title = "Long",
				Type = "Test",
				AssignedTo = null
			}
		};

		// act
		RoadmapGridAlgorithms.WorkItemGrid result = RoadmapGridAlgorithms.CreateWorkItemsGrid(0, iterations, workItems);

		// assert
		Assert.AreEqual(2, result.Rows.Count);
		WorkItemViewModel shortItem = result.WorkItemViewModels.Single(x => x.WorkItem == workItems[0]);
		WorkItemViewModel longItem = result.WorkItemViewModels.Single(x => x.WorkItem == workItems[1]);
		Assert.AreEqual(1, shortItem.RowIndex);
		Assert.AreEqual(1, shortItem.StartColumnIndex);
		Assert.AreEqual(2, shortItem.EndColumnIndexExclusive);
		Assert.AreEqual(0, longItem.RowIndex);
		Assert.AreEqual(0, longItem.StartColumnIndex);
		Assert.AreEqual(3, longItem.EndColumnIndexExclusive);
	}

	private static List<RoadmapWorkItem> GenerateWorkItems(Iteration iteration, int count)
	{
		var list = new List<RoadmapWorkItem>();
		for (var i = 0; i < count; i++)
		{
			list.Add(new RoadmapWorkItem
			{
				Iteration = iteration,
				State = "Test",
				Title = "Test",
				Type = "Test",
				AssignedTo = null
			});
		}
		return list;
	}
}