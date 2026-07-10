using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StylizedColorConfigSO", menuName = "ScriptableObjects/StylizedColorConfigSO")]
public class StylizedColorConfigSO : ScriptableObject
{
	public List<StylizedColorEntry> blockColors = new List<StylizedColorEntry>();

	public List<StylizedColorMaterialSourceEntry> exportSources = new List<StylizedColorMaterialSourceEntry>();

	public StylizedColorEntry GetColorEntry(EBlockColorType blockColorType)
	{
		foreach (StylizedColorEntry entry in blockColors)
		{
			if (entry == null || entry.BlockColorType != blockColorType)
			{
				continue;
			}
			return entry;
		}
		return new StylizedColorEntry();
	}
}
