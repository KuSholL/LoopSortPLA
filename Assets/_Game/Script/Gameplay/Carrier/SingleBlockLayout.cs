using System.Collections.Generic;
using UnityEngine;

public class SingleBlockLayout : CarrierBlockLayoutBase
{
    private readonly List<Block> _blocks = new List<Block>();

    public override List<Block> Blocks => _blocks;
    public override Transform Root => transform;

    public override Block GetBlockByIndex(int index)
    {
        return index == 0 && _blocks.Count > 0 ? _blocks[0] : null;
    }

    public override Block GetBlockAt(int index)
    {
        return GetBlockByIndex(index);
    }
}
