using System;
using System.Collections.Generic;

[Serializable]
public sealed class BlockData
{
	public EBlockColorType BlockColor;

	public List<BlockMechanicData> Mechanics = new List<BlockMechanicData>();
}
