using UnityEngine;

[CreateAssetMenu(fileName = "CarrierLevelConfigSO", menuName = "ScriptableObjects/Level/CarrierLevelConfigSO")]
public class CarrierConfigSO : ScriptableObject
{
    public Carrier Prefab;
    public ContainerMechanic ContainerMechanic;
    public Spawner Spawner;
    public BlockLinkVisual BlockLinkVisualPrefab;
    [Min(1)] public int Depth = 2;
    [Min(1)] public int BlockCount = 4;

    public int GetBlockCount()
    {
        return Mathf.Max(1, BlockCount);
    }
}
