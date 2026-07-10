using System.Collections.Generic;
using UnityEngine;

public sealed class RaycastHitDistanceComparer : IComparer<RaycastHit>
{
	public static readonly RaycastHitDistanceComparer Instance = new RaycastHitDistanceComparer();

	public int Compare(RaycastHit x, RaycastHit y)
	{
		return x.distance.CompareTo(y.distance);
	}
}
