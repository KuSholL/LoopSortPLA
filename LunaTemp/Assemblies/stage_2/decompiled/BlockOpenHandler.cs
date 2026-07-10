using System.Collections.Generic;
using UnityEngine;

public sealed class BlockOpenHandler
{
	public bool TryOpen(int currentCount, ref EBlockState state, int reservedReceiveCount)
	{
		if (state == EBlockState.Unloading)
		{
			return false;
		}
		if (reservedReceiveCount > 0)
		{
			return false;
		}
		if (state == EBlockState.Receiving)
		{
			state = EBlockState.Idle;
		}
		state = EBlockState.Unloading;
		return currentCount > 0;
	}

	public IReadOnlyList<BlockCubePayload> CreateHiddenCubePayloadSnapshot(int currentCount, EBlockColorType blockColorType, Color blockColor, Vector3 worldPos, Transform blockTransform = null, int maxCubes = 0)
	{
		List<BlockCubePayload> payloads = new List<BlockCubePayload>(currentCount);
		int total = ((maxCubes > 0) ? maxCubes : currentCount);
		for (int i = 0; i < currentCount; i++)
		{
			Vector3 cubePos = BlockZigzagOffsetCalculator.GetZigzagWorldPosition(i, total, worldPos, blockTransform);
			payloads.Add(new BlockCubePayload(cubePos, blockColor, blockColorType));
		}
		return payloads;
	}
}
