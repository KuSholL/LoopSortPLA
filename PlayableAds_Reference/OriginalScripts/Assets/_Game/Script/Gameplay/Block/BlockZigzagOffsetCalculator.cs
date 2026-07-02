using UnityEngine;

public static class BlockZigzagOffsetCalculator
{
    public static Vector3 GetZigzagWorldPosition(
        int cubeIndex,
        int totalCubes,
        Vector3 blockWorldCenter,
        Transform blockTransform)
    {
        if (totalCubes <= 1 || blockTransform == null)
            return blockWorldCenter;

        var grid = GetGridDimensions(totalCubes);
        var cols = grid.x;
        var rows = grid.y;

        var layerSize = cols * rows;
        var indexInLayer = cubeIndex % layerSize;

        var row = indexInLayer / cols;
        var col = indexInLayer % cols;

        if (row % 2 == 1)
        {
            col = cols - 1 - col;
        }

        // Cần lấy kích thước thật của block thông qua BoxCollider để tính khoảng cách linh hoạt
        var blockSize = Vector3.one;
        var blockCollider = blockTransform.GetComponent<BoxCollider>();
        if (blockCollider != null)
        {
            blockSize = blockCollider.size;
        }

        // Tỷ lệ lấp đầy khoảng không gian bên trong collider để tránh hạt bị lấn ra ngoài mép
        const float fillFactor = 0.75f;

        var spacingX = cols > 1 ? (blockSize.x * fillFactor) / (cols - 1) : 0f;
        var spacingY = rows > 1 ? (blockSize.y * fillFactor) / (rows - 1) : 0f;

        var offsetX = (col - (cols - 1) * 0.5f) * spacingX;
        var offsetY = (row - (rows - 1) * 0.5f) * spacingY;

        // Bỏ độ sâu (Z) để các hạt chỉ zic-zac phẳng trên mặt phẳng XY quanh animation pivot
        var localOffset = new Vector3(offsetX, offsetY, 0f);
        var worldOffset = blockTransform.TransformDirection(localOffset);

        return blockWorldCenter + worldOffset;
    }

    private static Vector2Int GetGridDimensions(int totalCubes)
    {
        if (totalCubes <= 0) return new Vector2Int(1, 1);

        var bestCols = 1;
        var bestRows = totalCubes;
        var bestGap = int.MaxValue;

        for (var r = 1; r <= totalCubes; r++)
        {
            var c = Mathf.CeilToInt((float)totalCubes / r);
            var gap = Mathf.Abs(c - r);
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
