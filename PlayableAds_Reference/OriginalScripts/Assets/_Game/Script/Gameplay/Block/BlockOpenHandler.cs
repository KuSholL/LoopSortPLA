using System.Collections.Generic;
using UnityEngine;

public sealed class BlockOpenHandler
{
    #region Open Flow

    public bool TryOpen(int currentCount, ref EBlockState state, int reservedReceiveCount)
    {
        if (state == EBlockState.Unloading) return false;
        if (reservedReceiveCount > 0) return false;

        if (state == EBlockState.Receiving)
        {
            state = EBlockState.Idle;
        }

        state = EBlockState.Unloading;
        return currentCount > 0;
    }

    public IReadOnlyList<BlockCubePayload> CreateHiddenCubePayloadSnapshot(
        int currentCount,
        EBlockColorType blockColorType,
        Color blockColor,
        Vector3 worldPos,
        Transform blockTransform = null,
        int maxCubes = 0)
    {
        var payloads = new List<BlockCubePayload>(currentCount);
        var total = maxCubes > 0 ? maxCubes : currentCount;
        for (var i = 0; i < currentCount; i++)
        {
            var cubePos = BlockZigzagOffsetCalculator.GetZigzagWorldPosition(
                i, total, worldPos, blockTransform);
            payloads.Add(new BlockCubePayload(cubePos, blockColor, blockColorType));
        }

        return payloads;
    }

    #endregion
}
