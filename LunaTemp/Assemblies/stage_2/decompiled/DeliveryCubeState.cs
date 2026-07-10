using UnityEngine;

public sealed class DeliveryCubeState
{
	public readonly Cube Cube;

	public readonly CarrierBase SourceCarrier;

	public readonly EBlockColorType BlockColorType;

	public readonly Color Color;

	public readonly int UndoBatchId;

	public bool IsPickedUp;

	public float PreviousProgress;

	public float PreviousProgressCorner;

	public DeliveryCubeState(Cube cube, CarrierBase sourceCarrier, EBlockColorType blockColorType, Color color, int undoBatchId)
	{
		Cube = cube;
		SourceCarrier = sourceCarrier;
		BlockColorType = blockColorType;
		Color = color;
		UndoBatchId = undoBatchId;
	}
}
