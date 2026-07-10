using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ColorConfigSO", menuName = "ScriptableObjects/ColorConfigSO")]
public class ColorConfigSO : ScriptableObject
{
	public List<ColorEntry> blockColors = new List<ColorEntry>();

	public List<ColorMaterialSourceEntry> exportSources = new List<ColorMaterialSourceEntry>();

	public ColorEntry GetColorEntry(EBlockColorType blockColorType)
	{
		foreach (ColorEntry entry in blockColors)
		{
			if (entry == null || entry.BlockColorType != blockColorType)
			{
				continue;
			}
			return entry;
		}
		return PlayableColorFallback.CreateColorEntry(blockColorType);
	}
}
