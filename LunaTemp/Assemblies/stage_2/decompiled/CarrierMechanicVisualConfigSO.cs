using UnityEngine;

[CreateAssetMenu(fileName = "CarrierMechanicVisualConfigSO", menuName = "ScriptableObjects/CarrierMechanicVisualConfigSO")]
public class CarrierMechanicVisualConfigSO : ScriptableObject
{
	[SerializeField]
	private CarrierVisualPrefabEntry[] visualPrefabs;

	public CarrierMechanicVisual GetVisualPrefab(ECarrierVisualKind kind)
	{
		if (visualPrefabs == null)
		{
			return null;
		}
		CarrierVisualPrefabEntry[] array = visualPrefabs;
		foreach (CarrierVisualPrefabEntry entry in array)
		{
			if (entry != null && entry.Kind == kind)
			{
				return (entry.Prefab != null) ? entry.Prefab.GetComponent<CarrierMechanicVisual>() : null;
			}
		}
		return null;
	}
}
