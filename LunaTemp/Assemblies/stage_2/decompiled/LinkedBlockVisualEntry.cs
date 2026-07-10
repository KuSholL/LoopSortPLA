using System;
using UnityEngine;

[Serializable]
public sealed class LinkedBlockVisualEntry
{
	[Min(2f)]
	public int BlockCount = 2;

	public LinkedBlockVisual Prefab;

	public Vector3 LocalOffset;
}
