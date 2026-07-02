using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "LevelConfigSO", menuName = "ScriptableObjects/LevelConfig")]
public class LevelConfigSO : ScriptableObject
{
    public List<LevelData> LevelDataList;
    public int MainMenuRedirectLevel;

    public LevelData GetLevelData(int levelId)
    {
        if (LevelDataList == null || LevelDataList.Count == 0) return null;
        var levelToLoad = GetLevelToLoad(levelId);
        for (var i = 0; i < LevelDataList.Count; i++)
        {
            var levelData = LevelDataList[i];
            if (levelData != null && levelData.LevelId == levelToLoad) return levelData;
        }
        return null;
    }

    public int GetLevelToLoad(int playerCurrentLevel)
    {
        if (LevelDataList == null || LevelDataList.Count == 0) return 0;
        return (Mathf.Max(1, playerCurrentLevel) - 1) % LevelDataList.Count + 1;
    }
    private void ArrangeLevelDataList()
    {
#if UNITY_EDITOR
        if (LevelDataList == null || LevelDataList.Count <= 0) return;
        Undo.RecordObject(this, "Arrange Level Data List");
        LevelDataList = LevelDataList
            .OrderBy(GetLevelDataSortIndex)
            .ThenBy(levelData => levelData == null ? string.Empty : levelData.name)
            .ToList();
        for (var i = 0; i < LevelDataList.Count; i++)
        {
            if (LevelDataList[i] == null) continue;
            Undo.RecordObject(LevelDataList[i], "Update Level Id");
            LevelDataList[i].LevelId = i + 1;
            EditorUtility.SetDirty(LevelDataList[i]);
        }
        EditorUtility.SetDirty(this);
#endif
    }

    private static int GetLevelDataSortIndex(LevelData levelData)
    {
        if (levelData == null) return int.MaxValue;
        var suffix = GetTrailingNumber(levelData.name);
        return suffix >= 0 ? suffix : int.MaxValue;
    }

    private static int GetTrailingNumber(string value)
    {
        if (string.IsNullOrEmpty(value)) return -1;
        var index = value.Length - 1;
        while (index >= 0 && char.IsDigit(value[index])) index--;
        var numberStart = index + 1;
        if (numberStart >= value.Length) return -1;
        int number;
        return int.TryParse(value.Substring(numberStart), out number) ? number : -1;
    }
}
