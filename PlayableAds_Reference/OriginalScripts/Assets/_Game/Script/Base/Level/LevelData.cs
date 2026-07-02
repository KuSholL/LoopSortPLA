using System;
using System.Collections.Generic;
using System.Linq;
using MemoryPack;
using UnityEngine;
using UnityEngine.Splines;

[CreateAssetMenu(fileName = "LevelData", menuName = "ScriptableObjects/Level/LevelData")]
public class LevelData : ScriptableObject
{
    public int LevelId;
    public int BaselineCapacity;
    public int Capacity;
    public int OrthographicSize = 11;
    public int GoldReward;
    public LevelType LevelType;
    public CarrierLayoutData CarrierLayout = new();
    public SplinePathData SplineLayout = new();
}

public enum LevelType
{
    None = 0,
    Normal = 1,
    Medium = 2,
    Hard = 3,
    Extreme = 4
}


[Serializable]
[MemoryPackable]
public sealed partial class CarrierLayoutData
{
    public List<CarrierStackData> Carriers = new();
    public List<CarrierStackData> BoosterCarriers = new();
    public List<ContainerLevelData> Containers = new();
}

[Serializable]
[MemoryPackable]
public sealed partial class CarrierStackData
{
    [Range(0f, 1f)] public float Progress;
    public Vector3 Position;
    public float RotationY;
    public List<BlockData> Blocks = new();
    public List<CarrierMechanicData> Mechanics = new();

}

[Serializable]
[MemoryPackable]
public sealed partial class ContainerLevelData
{
    public int ContainerId = -1;
    public EBlockColorType UnlockColor = EBlockColorType.None;
    public Vector3 Position;
    public float RotationY;
    public float ScaleXZ = 1f;
    public List<int> CarrierIndexes = new();
}

[Serializable]
[MemoryPackable]
public sealed partial class CarrierMechanicData
{
    public ECarrierMechanic Type;
    public EBlockColorType UnlockColor = EBlockColorType.Red;
    public EBlockColorType TargetColor = EBlockColorType.None;
}

[Serializable]
[MemoryPackable]
public sealed partial class BlockData
{
    public EBlockColorType BlockColor;
    public List<BlockMechanicData> Mechanics = new();
}

[Serializable]
[MemoryPackable]
public sealed partial class BlockMechanicData
{
    public EBlockMechanic Type;
    public int ContainerId = -1;
    public EBlockColorType KeyColor = EBlockColorType.None;
    public int LinkGroupId = -1;
    public int SwapGroupId = -1;
}

public enum EBlockMechanic
{
    HiddenBlock,
    KeyUnlockContainer,
    BlockLink,
    SwappingBlock,
}

public enum ECarrierMechanic
{
    HiddenByColor,
    OneWay,
    SpecialColorReceiver,
    Spawner,
}

[Serializable]
[MemoryPackable]
public sealed partial class SplinePathData
{
    public bool Closed;
    public List<SplinePointData> Nodes = new();

    public List<SplinePointData> GetMapPointsInOrder()
    {
        return Nodes
            .Where(node => node != null)
            .OrderBy(node => node.MapPointId)
            .ToList();
    }
}

[Serializable]
[MemoryPackable]
public sealed partial class SplinePointData
{
    public int MapPointId;
    public Vector2 GridPosition;
    public TangentMode TangentMode;
    public Vector3 TangentInValue;
    public Vector3 TangentOutValue;
    public Vector3 Rotation;
}
