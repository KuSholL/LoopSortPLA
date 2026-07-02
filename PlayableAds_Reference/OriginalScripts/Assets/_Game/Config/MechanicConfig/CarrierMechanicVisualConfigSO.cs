using UnityEngine;

[CreateAssetMenu(fileName = "CarrierMechanicVisualConfigSO", menuName = "ScriptableObjects/CarrierMechanicVisualConfigSO")]
public class CarrierMechanicVisualConfigSO : ScriptableObject
{
    [SerializeField] private CarrierVisualPrefabEntry[] visualPrefabs;

    public CarrierMechanicVisual GetVisualPrefab(ECarrierVisualKind kind)
    {
        if (visualPrefabs == null) return null;
        foreach (var entry in visualPrefabs)
        {
            if (entry == null || entry.Kind != kind) continue;
            return entry.Prefab;
        }

        return null;
    }
}

[System.Serializable]
public class CarrierVisualPrefabEntry
{
    public ECarrierVisualKind Kind;
    public CarrierMechanicVisual Prefab;
}
