using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelConfigSO", menuName = "ScriptableObjects/LevelConfig")]
public class LevelConfigSO : ScriptableObject
{
	public List<LevelData> LevelDataList;

	public int MainMenuRedirectLevel;

	public LevelData GetLevelData(int levelId)
	{
		if (LevelDataList == null || LevelDataList.Count == 0)
		{
			return null;
		}
		int levelToLoad = GetLevelToLoad(levelId);
		for (int i = 0; i < LevelDataList.Count; i++)
		{
			LevelData levelData = LevelDataList[i];
			if (levelData != null && levelData.LevelId == levelToLoad)
			{
				return levelData;
			}
		}
		return null;
	}

	public int GetLevelToLoad(int playerCurrentLevel)
	{
		if (LevelDataList == null || LevelDataList.Count == 0)
		{
			return 0;
		}
		return (Mathf.Max(1, playerCurrentLevel) - 1) % LevelDataList.Count + 1;
	}

	private void ArrangeLevelDataList()
	{
	}

	private static int GetLevelDataSortIndex(LevelData levelData)
	{
		if (levelData == null)
		{
			return int.MaxValue;
		}
		int suffix = GetTrailingNumber(levelData.name);
		return (suffix >= 0) ? suffix : int.MaxValue;
	}

	private static int GetTrailingNumber(string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return -1;
		}
		int index = value.Length - 1;
		while (index >= 0 && char.IsDigit(value[index]))
		{
			index--;
		}
		int numberStart = index + 1;
		if (numberStart >= value.Length)
		{
			return -1;
		}
		int number;
		return int.TryParse(value.Substring(numberStart), out number) ? number : (-1);
	}
}
