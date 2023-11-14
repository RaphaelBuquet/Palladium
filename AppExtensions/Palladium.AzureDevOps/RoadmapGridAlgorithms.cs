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

	public struct WorkItemGrid
	{
		public required List<WorkItemViewModel> WorkItemViewModels;
		public required List<GridLength> Rows;
	}

	public readonly struct GridOccupancy
	{
		/// <summary>
		///     Each list represents the occupancy of a column. Each ulong in the list represents 64 cells (bitfield).
		/// </summary>
		private readonly List<List<ulong>> grid;

		/// <summary>
		///     Maps iterations to their locations.
		/// </summary>
		private readonly Dictionary<IterationViewModel, (int columnStartIndex, int columnEndIndex)> iterationLocations;

		public GridOccupancy(IReadOnlyList<IterationViewModel> iterations)
		{
			iterationLocations = iterations.ToDictionary(x => x, x =>
				(columnStartIndex: x.StartColumnIndex, columnEndIndex: x.EndColumnIndexExclusive)
			);
			int columnCount = 0;
			foreach (var iteration in iterations)
			{
				columnCount = int.Max(columnCount, iteration.EndColumnIndexExclusive);
			}
			grid = new List<List<ulong>>(columnCount);
			for (var i = 0; i < columnCount; i++)
			{
				grid.Add(new List<ulong>() { 0 });
			}
		}

		private static bool IsOccupied(List<ulong> column, int bitfieldIndex, int bitIndex)
		{
			ulong bitfield = column[bitfieldIndex];
			return (bitfield & (1UL << bitIndex)) != 0;
		}

		private static void MakeOccupied(List<ulong> column, int bitfieldIndex, int bitIndex)
		{
			column[bitfieldIndex] |= 1UL << bitIndex;
		}

		public int FindRowForWorkItem(IterationViewModel workItemIteration)
		{
			(int columnStartIndex, int columnEndIndex) = iterationLocations[workItemIteration];

			var targetColumn = grid[columnStartIndex];
			var found = false;
			var bitfieldIndex = 0;
			var bitIndex = 0;

			// look for the first available slot from the start
			// using the start instead of the end because multi-column items may leave holes otherwise.
			while (!found)
			{
				if (bitfieldIndex == targetColumn.Count)
				{
					targetColumn.Add(0); // allocate another item if capacity is reached
				}
				else if (bitIndex == 64)
				{
					bitfieldIndex++;
					bitIndex = 0;
				}
				else
				{
					bool isOccupied = IsOccupied(targetColumn, bitfieldIndex, bitIndex);
					if (isOccupied)
					{
						bitIndex++;
					}
					else
					{
						// try to reserve other columns
						var available = true;
						int columnIndex = columnStartIndex + 1;
						while (available && columnIndex < columnEndIndex)
						{
							var column = grid[columnIndex];
							bool adjacentCellOccupied = IsOccupied(column, bitfieldIndex, bitIndex);
							if (adjacentCellOccupied)
							{
								available = false;
							}
							else
							{
								columnIndex++;
							}
						}
						if (available)
						{
							found = true;
						}
						else
						{
							bitIndex++;
						}
					}
				}
			}

			// paint the grid
			for (int columnIndex = columnStartIndex; columnIndex < columnEndIndex; columnIndex++)
			{
				MakeOccupied(grid[columnIndex], bitfieldIndex, bitIndex);
			}

			return ConvertBitfieldIndexToRowIndex(bitfieldIndex, bitIndex);
		}

		public int CalculateRowCount()
		{
			var max = 0;
			for (int columnIndex = grid.Count - 1; columnIndex >= 0 ; columnIndex--)
			{
				var column = grid[columnIndex];
				ulong lastBitfield = column[^1];
				int highestBitIndex = 64 - 1 - System.Numerics.BitOperations.LeadingZeroCount(lastBitfield);
				int rowCount = ConvertBitfieldIndexToRowIndex(column.Count - 1, highestBitIndex) + 1;
				if (rowCount > max) max = rowCount;
			}
			return max;
		}

		private static int ConvertBitfieldIndexToRowIndex(int bitfieldIndex, int bitIndex)
		{
			return bitfieldIndex * 64 + bitIndex;
		}
	}

	public static IterationsGrid CreateIterationsGrid(IReadOnlyList<Iteration> iterations)
	{
		// break down iterations into two objects, to represent the start and end of the iterations
		var columnChanges = new List<ColumnChange>();
		foreach (Iteration iteration in iterations)
		{
			int level = CalculateIterationLevel(iteration.IterationPath);
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
			foreach (ColumnChange columnChange in orderedColumnChanges)
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
			foreach (ColumnChange columnChange in orderedColumnChanges)
			{
				if (currentColumnTime != columnChange.ChangeDate)
				{
					Debug.Assert(columnChange.ChangeDate > currentColumnTime);
					if (currentColumnIndex >= 0)
					{
						double elapsedDays = (columnChange.ChangeDate - currentColumnTime).TotalDays;
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

	public static WorkItemGrid CreateWorkItemsGrid(
		int rowOffset,
		IReadOnlyList<IterationViewModel> iterationViewModels,
		IReadOnlyList<RoadmapWorkItem> roadmapWorkItems)
	{
		var lookup = iterationViewModels.ToDictionary(x => x.Iteration);

		var gridOccupancy = new GridOccupancy(iterationViewModels);

		var vms = new List<WorkItemViewModel>();
		var orderedItems = roadmapWorkItems
			.OrderByDescending(x =>  x.Iteration.EndDate.Subtract(x.Iteration.StartDate));
		foreach (RoadmapWorkItem workItem in orderedItems)
		{
			IterationViewModel columnInfo = lookup[workItem.Iteration];
			int rowForWorkItem = gridOccupancy.FindRowForWorkItem(columnInfo);

			var vm = new WorkItemViewModel(workItem)
			{
				StartColumnIndex = columnInfo.StartColumnIndex,
				EndColumnIndexExclusive = columnInfo.EndColumnIndexExclusive,
				RowIndex = rowForWorkItem + rowOffset
			};
			vms.Add(vm);
		}

		int rowCount = gridOccupancy.CalculateRowCount();
		return new WorkItemGrid()
		{
			Rows = new List<GridLength>(Enumerable.Repeat(GridLength.Auto, rowCount)),
			WorkItemViewModels = vms
		};
	}

	public static int CalculateIterationLevel(string argPath)
	{
		return argPath.Count(x => x == '\\');
	}
}