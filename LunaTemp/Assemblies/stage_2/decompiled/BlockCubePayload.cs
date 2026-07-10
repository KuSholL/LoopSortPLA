using UnityEngine;

public struct BlockCubePayload
{
	public readonly Vector3 WorldPosition;

	public readonly Color Color;

	public readonly EBlockColorType ColorType;

	public BlockCubePayload(Vector3 worldPosition, Color color, EBlockColorType colorType)
	{
		WorldPosition = worldPosition;
		Color = color;
		ColorType = colorType;
	}
}
