using UnityEngine;

public static class CarrierGridUtility
{
	private const int BaselineColumns = 2;

	private const int BaselineRows = 2;

	private const int BaselineDepth = 2;

	public static int GetDepth(CarrierConfigSO config)
	{
		return Mathf.Max(1, (config != null) ? config.Depth : 2);
	}

	public static int GetBaselineCubePerBlock(CarrierConfigSO config)
	{
		return 4 * GetDepth(config);
	}

	public static int GetCubePerBlock(LevelData level, CarrierConfigSO config)
	{
		Vector2Int grid = GetGridSize(level);
		return grid.x * grid.y * GetDepth(config);
	}

	public static Vector2Int GetGridSize(LevelData level)
	{
		return GetBestGridSize(GetLayerCellCount(level));
	}

	private static int GetLayerCellCount(LevelData level)
	{
		int baseline = ((level != null) ? Mathf.Max(1, level.BaselineCapacity) : 0);
		int capacity = ((level != null) ? Mathf.Max(1, level.Capacity) : 0);
		if (baseline <= 0 || capacity <= 0)
		{
			return 4;
		}
		return Mathf.Max(1, Mathf.RoundToInt((float)baseline * 4f / (float)capacity));
	}

	private static Vector2Int GetBestGridSize(int value)
	{
		Vector2Int best = new Vector2Int(2, 2);
		int bestArea = int.MaxValue;
		int bestGap = int.MaxValue;
		for (int row = 2; row <= value; row++)
		{
			UpdateBestGrid(value, row, ref best, ref bestArea, ref bestGap);
		}
		return best;
	}

	private static void UpdateBestGrid(int value, int row, ref Vector2Int best, ref int bestArea, ref int bestGap)
	{
		int col = Mathf.Max(2, Mathf.CeilToInt((float)value / (float)row));
		int area = col * row;
		int gap = Mathf.Abs(col - row);
		if (area <= bestArea && (area != bestArea || gap < bestGap))
		{
			best = new Vector2Int(col, row);
			bestArea = area;
			bestGap = gap;
		}
	}
}
