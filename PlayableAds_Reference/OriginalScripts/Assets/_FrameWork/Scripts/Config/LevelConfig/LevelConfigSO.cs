using System.Collections.Generic;
using System.Linq;
using Alchemy.Inspector;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "LevelConfigSO", menuName = "ScriptableObjects/LevelConfig")]
public class LevelConfigSO : ScriptableObject
{
    public List<LevelData> LevelDataList;
    public int MainMenuRedirectLevel;

    public LevelData GetLevelData(int levelId)
    {
        var levelToLoad = GetLevelToLoad(levelId);
        var targetLevel = LevelDataList.FirstOrDefault(x => x.LevelId == levelToLoad);
        return targetLevel;
    }

    public int GetLevelToLoad(int playerCurrentLevel)
    {
        int levelStartLoop = PlayerPrefs.GetInt(RemoteConfigDatas.LevelStartLoopKey, RemoteConfigDatas.LevelStartLoopValue);
        int levelToLoad = playerCurrentLevel;
        var levelCount = LevelDataList.Count;
        if (levelToLoad > levelCount)
        {
            levelToLoad = (levelToLoad - levelStartLoop) % (LevelDataList.Count - levelStartLoop + 1) + levelStartLoop;
        }

        return levelToLoad;
    }

    [Button]
    private void ArrangeLevelDataList()
    {
#if UNITY_EDITOR
        if (LevelDataList is not { Count: > 0 }) return;
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
        return int.TryParse(value[numberStart..], out var number) ? number : -1;
    }
}
