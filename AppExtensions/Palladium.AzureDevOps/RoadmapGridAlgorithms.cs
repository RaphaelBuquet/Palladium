using System.Collections.Immutable;
using System.Diagnostics;
using Avalonia.Controls;
using AzureDevOpsTools;

namespace Palladium.AzureDevOps;

public static class RoadmapGridAlgorithms
{
	public const int IterationRowOffset = 0;
	public const int IterationColumnOffset = 0;

	private struct ColumnChange
	{
		public Iteration Iteration;
		public bool IsStart;
		public DateTime ChangeDate;
		public int Level;
	}
	
	public struct IterationsGrid
	{
		public required List<IterationViewModel> IterationViewModels;
		public required List<GridLength> Columns;
		public required List<GridLength> Rows;
	}

	public static IterationsGrid CreateIterationsGrid(IReadOnlyList<Iteration> iterations)
	{
		// break down iterations into two objects, to represent the start and end of the iterations
		var columnChanges = new List<ColumnChange>();
		foreach (var iteration in iterations)
		{
			var level = CalculateIterationLevel(iteration.IterationPath);
			columnChanges.Add(new ColumnChange()
			{
				Iteration = iteration,
				ChangeDate = iteration.StartDate,
				IsStart = true,
				Level = level
			});
			columnChanges.Add(new ColumnChange()
			{
				Iteration = iteration,
				ChangeDate = iteration.EndDate,
				IsStart = false,
				Level = level
			});
		}
		
		// order items
		var orderedColumnChanges = columnChanges
			.OrderBy(x => x.ChangeDate) // order by date: old->new
			.ThenByDescending(x => x.IsStart) // order by: start->end (true first, false second)
			.ThenBy(x => x.Level) // order by level: 0->1 (high level first)
			.ThenBy(x => x.Iteration.DisplayName)
			.ToImmutableList(); // finally order by name for determinism

		// compute overlaps, which later be used to compute row indexes
		var maxOverlaps = new Dictionary<int, int>();
		{
			var overlaps = new Dictionary<int, int>(); // a level is represented by the index of the item, the item represents the number of rows
			foreach (var columnChange in orderedColumnChanges)
			{
				if (columnChange.IsStart)
				{
					overlaps.TryAdd(columnChange.Level, 0);
					maxOverlaps.TryAdd(columnChange.Level, 0);

					int overlapCount = ++overlaps[columnChange.Level];
					if (overlapCount > maxOverlaps[columnChange.Level])
					{
						maxOverlaps[columnChange.Level] = overlapCount;
					}
				}
				else
				{
					--overlaps[columnChange.Level];
				}
			}
		}

		// compute row index offsets
		var rows = new List<GridLength>();
		var levelsOffset = new Dictionary<int, int>();
		{
			var currentOffset = 0;
			foreach (var pair in maxOverlaps.OrderBy(pair => pair.Key))
			{
				levelsOffset[pair.Key] = currentOffset;
				currentOffset += pair.Value;
				rows.Add(GridLength.Auto);
			}
		}

		var items = new Dictionary<Iteration, IterationViewModel>();
		var columns = new List<GridLength>();
		{
			var overlaps = new Dictionary<int, int>(); // a level is represented by the index of the item, the item represents the number of rows
			int currentColumnIndex = IterationColumnOffset - 1;
			var currentColumnTime = new DateTime(0);
			foreach (var columnChange in orderedColumnChanges)
			{
				if (currentColumnTime != columnChange.ChangeDate)
				{
					Debug.Assert(columnChange.ChangeDate > currentColumnTime);
					if (currentColumnIndex >= 0)
					{
						var elapsedDays = (columnChange.ChangeDate - currentColumnTime).TotalDays;
						columns.Add(new GridLength(elapsedDays, GridUnitType.Star));
					}
					++currentColumnIndex;
					currentColumnTime = columnChange.ChangeDate;
				}

				if (columnChange.IsStart)
				{
					overlaps.TryAdd(columnChange.Level, 0);
					int overlapCount = ++overlaps[columnChange.Level];
					int rowIndex = IterationRowOffset + overlapCount - 1 + levelsOffset[columnChange.Level];
					int columnIndex = currentColumnIndex;
					items[columnChange.Iteration] = new IterationViewModel(columnChange.Iteration)
					{
						StartColumnIndex = columnIndex,
						RowIndex = rowIndex
					};
				}
				else
				{
					--overlaps[columnChange.Level];
					IterationViewModel vm = items[columnChange.Iteration];
					vm.EndColumnIndexExclusive = currentColumnIndex;
				}
			}
		}

		return new IterationsGrid()
		{
			IterationViewModels = items.Values.ToList(),
			Columns = columns,
			Rows = rows
		};
	}

	public static int CalculateIterationLevel(string argPath)
	{
		return argPath.Count(x => x == '\\');
	}

	public static double InverseLerp(long a, long b, long value)
	{
		if (a != b)
			return (double)(value - a) / (b - a);
		else
			return 0.0;
	}
}