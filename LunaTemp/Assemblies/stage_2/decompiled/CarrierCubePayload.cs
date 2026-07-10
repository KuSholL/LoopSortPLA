using UnityEngine;

public struct CarrierCubePayload
{
	public readonly Block SourceBlock;

	public readonly Vector3 StartWorldPosition;

	public readonly Color Color;

	public readonly EBlockColorType BlockColorType;

	public CarrierCubePayload(Block sourceBlock, Vector3 startWorldPosition, Color color, EBlockColorType blockColorType)
	{
		SourceBlock = sourceBlock;
		StartWorldPosition = startWorldPosition;
		Color = color;
		BlockColorType = blockColorType;
	}
}
