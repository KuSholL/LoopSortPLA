using System.Collections.Generic;
using UnityEngine;

public sealed class BlockRuntimeData
{
	public bool HasContent;

	public EBlockColorType BlockColorType;

	public Color Color;

	public Color ShadowColor;

	public int CubeCount;

	public bool IsHiddenRevealed;

	public bool IsContainerKeyConsumed;

	public List<BlockMechanicData> Mechanics = new List<BlockMechanicData>();

	public bool IsSwappingActive = true;

	public int SwapGroupId = -1;

	public EBlockColorType SwapArrowColorType = EBlockColorType.None;
}
