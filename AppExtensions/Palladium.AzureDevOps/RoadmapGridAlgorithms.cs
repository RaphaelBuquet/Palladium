using System.Collections.Immutable;
using System.Diagnostics;
using Avalonia.Controls;
using AzureDevOpsTools;

namespace Palladium.AzureDevOps;

public static class RoadmapGridAlgorithms
{
	public const int IterationRowOffset = 0;
	public const int IterationColumnOffset = 0;

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


		// firstly compute the columns, so that we can later generate a grid to place the blocks on 
		var items = new Dictionary<Iteration, IterationViewModel>();
		var columns = new List<GridLength>();
		{
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
					int columnIndex = currentColumnIndex;
					items[columnChange.Iteration] = new IterationViewModel(columnChange.Iteration)
					{
						StartColumnIndex = columnIndex
					};
				}
				else
				{
					IterationViewModel vm = items[columnChange.Iteration];
					vm.EndColumnIndexExclusive = currentColumnIndex;
				}
			}
		}

		var iterationViewModels = items.Values.ToList();

		var columnCount = 0;
		if (iterationViewModels.Count > 0)
		{
			columnCount = iterationViewModels.Max(x => x.EndColumnIndexExclusive);
		}

		// now create grids. each level is its own grid.
		var levels = orderedColumnChanges
			.Select(x => x.Level)
			.Distinct()
			.OrderBy(x => x) // shortest paths first
			.ToList();
		var rowCount = 0;
		foreach (int level in levels)
		{
			var gridOccupancy = new GridOccupancy(columnCount);
			var gridIterations = orderedColumnChanges
				.Where(x => x.Level == level && x.IsStart)
				.Select(x => x.Iteration);
			var levelGridRowCount = 0;
			foreach (Iteration iteration in gridIterations)
			{
				IterationViewModel vm = items[iteration];
				int rowIndexInLevelGrid = gridOccupancy.FindRowForItem(vm.StartColumnIndex, vm.EndColumnIndexExclusive);
				vm.RowIndex = rowIndexInLevelGrid + rowCount;
				levelGridRowCount = int.Max(levelGridRowCount, rowIndexInLevelGrid + 1);
			}

			rowCount += levelGridRowCount;
		}

		return new IterationsGrid()
		{
			IterationViewModels = iterationViewModels,
			Columns = columns,
			Rows = Enumerable.Repeat(GridLength.Auto, rowCount).ToList()
		};
	}

	public static WorkItemGrid CreateWorkItemsGrid(
		int rowOffset,
		IReadOnlyList<IterationViewModel> iterationViewModels,
		IReadOnlyList<RoadmapWorkItem> roadmapWorkItems)
	{
		var lookup = iterationViewModels.ToDictionary(x => x.Iteration);

		var columnCount = 0;
		foreach (IterationViewModel iteration in iterationViewModels)
		{
			columnCount = int.Max(columnCount, iteration.EndColumnIndexExclusive);
		}
		var gridOccupancy = new GridOccupancy(columnCount);

		var vms = new List<WorkItemViewModel>();
		var orderedItems = roadmapWorkItems
			.OrderByDescending(x =>  x.Iteration.EndDate.Subtract(x.Iteration.StartDate));
		foreach (RoadmapWorkItem workItem in orderedItems)
		{
			IterationViewModel columnInfo = lookup[workItem.Iteration];
			int rowForWorkItem = gridOccupancy.FindRowForItem(columnInfo.StartColumnIndex, columnInfo.EndColumnIndexExclusive);

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

	public static bool HasNoOverlaps(List<IterationViewModel> iterationViewModels)
	{
		var columnCount = 0;
		var rowCount = 0;
		foreach (IterationViewModel vm in iterationViewModels)
		{
			columnCount = int.Max(columnCount, vm.EndColumnIndexExclusive);
			rowCount = int.Max(rowCount, vm.RowIndex + 1);
		}

		var grid = new bool[rowCount, columnCount];

		var hasOverlap = false;

		foreach (IterationViewModel vm in iterationViewModels)
		{
			for (int columnIndex = vm.StartColumnIndex; columnIndex < vm.EndColumnIndexExclusive; columnIndex++)
			{
				if (grid[vm.RowIndex, columnIndex])
				{
					hasOverlap = true;
					break;
				}
				grid[vm.RowIndex, columnIndex] = true;
			}
		}

		return !hasOverlap;
	}

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

	public struct ColumnOccupancy
	{
		public readonly List<ulong> Bitfields = new () { 0 };
		public int NextAvailableBitfieldIndex = 0;
		public int NextAvailableBitIndex = 0;

		public ColumnOccupancy()
		{ }

		private void AddBitfield()
		{
			if (Bitfields.Count == NextAvailableBitfieldIndex + 1)
			{
				Bitfields.Add(0);
			}
			NextAvailableBitfieldIndex++;
			NextAvailableBitIndex = 0;
		}

		public void MakeOccupied(int bitfieldIndex, int bitIndex)
		{
			Bitfields[bitfieldIndex] |= 1UL << bitIndex;

			// update cache
			do
			{
				NextAvailableBitIndex++;
				if (NextAvailableBitIndex == 64)
				{
					AddBitfield();
				}
			} while (IsOccupied(NextAvailableBitfieldIndex, NextAvailableBitIndex));
		}

		public bool IsOccupied(int bitfieldIndex, int bitIndex)
		{
			ulong bitfield = Bitfields[bitfieldIndex];
			return (bitfield & (1UL << bitIndex)) != 0;
		}
	}

	/// <summary>
	///     Grid of items with a height of 1 row, but with a variable width. The grid will try to fill gaps.
	/// </summary>
	public readonly struct GridOccupancy
	{
		/// <summary>
		///     Each list represents the occupancy of a column. Each ulong in the list represents 64 cells (bitfield).
		/// </summary>
		private readonly ColumnOccupancy[] grid;

		public GridOccupancy(int columnCount)
		{
			grid = new ColumnOccupancy[columnCount];
			for (var i = 0; i < columnCount; i++)
			{
				grid[i] = new ColumnOccupancy();
			}
		}

		public int FindRowForItem(int columnStartIndex, int columnEndIndex)
		{
			ref ColumnOccupancy targetColumn = ref grid[columnStartIndex];
			var found = false;
			int bitfieldIndex = targetColumn.NextAvailableBitfieldIndex;
			int bitIndex = targetColumn.NextAvailableBitIndex;

			// look for the first available slot from the start
			// using the start instead of the end because multi-column items may leave holes otherwise.
			while (!found)
			{
				if (bitfieldIndex == targetColumn.Bitfields.Count)
				{
					targetColumn.Bitfields.Add(0); // allocate another item if capacity is reached
				}
				else if (bitIndex == 64)
				{
					bitfieldIndex++;
					bitIndex = 0;
				}
				else
				{
					bool isOccupied = targetColumn.IsOccupied(bitfieldIndex, bitIndex);
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
							bool adjacentCellOccupied = targetColumn.IsOccupied(bitfieldIndex, bitIndex);
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
				ref ColumnOccupancy column = ref grid[columnIndex];
				column.MakeOccupied(bitfieldIndex, bitIndex);
			}

			return ConvertBitfieldIndexToRowIndex(bitfieldIndex, bitIndex);
		}

		public int CalculateRowCount()
		{
			var max = 0;
			for (int columnIndex = grid.Length - 1; columnIndex >= 0 ; columnIndex--)
			{
				var column = grid[columnIndex];
				ulong lastBitfield = column.Bitfields[^1];
				int highestBitIndex = 64 - 1 - System.Numerics.BitOperations.LeadingZeroCount(lastBitfield);
				int rowCount = ConvertBitfieldIndexToRowIndex(column.Bitfields.Count - 1, highestBitIndex) + 1;
				if (rowCount > max) max = rowCount;
			}
			return max;
		}

		private static int ConvertBitfieldIndexToRowIndex(int bitfieldIndex, int bitIndex)
		{
			return bitfieldIndex * 64 + bitIndex;
		}
	}
}