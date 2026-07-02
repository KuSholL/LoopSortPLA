using System.Collections.Generic;
using UnityEngine;

public abstract class CarrierBlockLayoutBase : MonoBehaviour
{
    public abstract List<Block> Blocks { get; }
    public abstract Transform Root { get; }
    public abstract Block GetBlockByIndex(int index);
    public abstract Block GetBlockAt(int index);
}
