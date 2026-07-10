using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class ContainerLevelData
{
	public int ContainerId = -1;

	public EBlockColorType UnlockColor = EBlockColorType.None;

	public Vector3 Position;

	public float RotationY;

	public float ScaleXZ = 1f;

	public List<int> CarrierIndexes = new List<int>();
}
