using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class CarrierStackData
{
	[Range(0f, 1f)]
	public float Progress;

	public Vector3 Position;

	public float RotationY;

	public List<BlockData> Blocks = new List<BlockData>();

	public List<CarrierMechanicData> Mechanics = new List<CarrierMechanicData>();
}
