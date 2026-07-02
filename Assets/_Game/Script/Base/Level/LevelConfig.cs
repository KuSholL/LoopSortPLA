using System;

[Serializable]
public partial class LevelConfig
{
    public int LevelId;
    public int BaselineCapacity;
    public int Capacity;
    public int OrthographicSize;
    public int GoldReward;
    public LevelType LevelType;
    public CarrierLayoutData CarrierLayout;
    public SplinePathData SplineLayout;
}
