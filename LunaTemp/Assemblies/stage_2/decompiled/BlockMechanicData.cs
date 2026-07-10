using System;

[Serializable]
public sealed class BlockMechanicData
{
	public EBlockMechanic Type;

	public int ContainerId = -1;

	public EBlockColorType KeyColor = EBlockColorType.None;

	public int LinkGroupId = -1;

	public int SwapGroupId = -1;
}
