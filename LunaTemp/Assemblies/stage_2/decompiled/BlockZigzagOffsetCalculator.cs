using UnityEngine;

public static class BlockZigzagOffsetCalculator
{
	public static Vector3 GetZigzagWorldPosition(int cubeIndex, int totalCubes, Vector3 blockWorldCenter, Transform blockTransform)
	{
		if (totalCubes <= 1 || blockTransform == null)
		{
			return blockWorldCenter;
		}
		Vector2Int grid = GetGridDimensions(totalCubes);
		int cols = grid.x;
		int rows = grid.y;
		int layerSize = cols * rows;
		int indexInLayer = cubeIndex % layerSize;
		int row = indexInLayer / cols;
		int col = indexInLayer % cols;
		if (row % 2 == 1)
		{
			col = cols - 1 - col;
		}
		Vector3 blockSize = Vector3.one;
		BoxCollider blockCollider = blockTransform.GetComponent<BoxCollider>();
		if (blockCollider != null)
		{
			blockSize = blockCollider.size;
		}
		float spacingX = ((cols > 1) ? (blockSize.x * 0.75f / (float)(cols - 1)) : 0f);
		float spacingY = ((rows > 1) ? (blockSize.y * 0.75f / (float)(rows - 1)) : 0f);
		float offsetX = ((float)col - (float)(cols - 1) * 0.5f) * spacingX;
		float offsetY = ((float)row - (float)(rows - 1) * 0.5f) * spacingY;
		Vector3 worldOffset = blockTransform.right * offsetX + blockTransform.up * offsetY;
		return blockWorldCenter + worldOffset;
	}

	private static Vector2Int GetGridDimensions(int totalCubes)
	{
		if (totalCubes <= 0)
		{
			return new Vector2Int(1, 1);
		}
		int bestCols = 1;
		int bestRows = totalCubes;
		int bestGap = int.MaxValue;
		for (int r = 1; r <= totalCubes; r++)
		{
			int c = Mathf.CeilToInt((float)totalCubes / (float)r);
			int gap = Mathf.Abs(c - r);
			if (c * r >= totalCubes && gap < bestGap)
			{
				bestCols = c;
				bestRows = r;
				bestGap = gap;
			}
		}
		return new Vector2Int(bestCols, bestRows);
	}
}
