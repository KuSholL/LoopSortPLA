using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CarrierLinkedBlockVisualConfigSO", menuName = "ScriptableObjects/Carrier/Linked Block Visual Config")]
public sealed class CarrierLinkedBlockVisualConfigSO : ScriptableObject
{
	[SerializeField]
	private List<LinkedBlockVisualEntry> linkedVisuals = new List<LinkedBlockVisualEntry>();

	public LinkedBlockVisualEntry GetEntry(int blockCount)
	{
		if (linkedVisuals == null)
		{
			return null;
		}
		for (int i = 0; i < linkedVisuals.Count; i++)
		{
			LinkedBlockVisualEntry entry = linkedVisuals[i];
			if (entry != null && entry.BlockCount == blockCount)
			{
				return entry;
			}
		}
		return null;
	}
}
