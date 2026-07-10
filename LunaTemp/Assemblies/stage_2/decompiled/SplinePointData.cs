using System;
using UnityEngine;

[Serializable]
public sealed class SplinePointData
{
	public int MapPointId;

	public Vector2 GridPosition;

	public TangentMode TangentMode;

	public Vector3 TangentInValue;

	public Vector3 TangentOutValue;

	public Vector3 Rotation;
}
