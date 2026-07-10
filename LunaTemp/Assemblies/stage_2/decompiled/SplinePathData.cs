using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public sealed class SplinePathData
{
	public bool Closed;

	public List<SplinePointData> Nodes = new List<SplinePointData>();

	public List<SplinePointData> GetMapPointsInOrder()
	{
		return (from node in Nodes
			where node != null
			orderby node.MapPointId
			select node).ToList();
	}
}
