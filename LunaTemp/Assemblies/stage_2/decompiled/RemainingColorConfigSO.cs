using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RemainingColorConfigSO", menuName = "ScriptableObjects/RemainingColorConfigSO")]
public class RemainingColorConfigSO : ScriptableObject
{
	public Color noneColor = Color.white;

	public List<RemainingColorEntry> remainingColorEntries;

	public RemainingColorEntry GetColorEntry(EBlockColorType blockColorType)
	{
		if (remainingColorEntries == null)
		{
			return null;
		}
		return remainingColorEntries.Find((RemainingColorEntry x) => x.BlockColorType == blockColorType);
	}
}
