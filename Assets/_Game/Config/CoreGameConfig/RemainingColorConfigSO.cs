using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RemainingColorConfigSO", menuName = "ScriptableObjects/RemainingColorConfigSO")]
public class RemainingColorConfigSO : ScriptableObject
{
    public Color noneColor = Color.white;
    public List<RemainingColorEntry> remainingColorEntries;

    public RemainingColorEntry GetColorEntry(EBlockColorType blockColorType)
    {
        if (remainingColorEntries == null) return null;
        return remainingColorEntries.Find(x => x.BlockColorType == blockColorType);
    }
}

[Serializable]
public class RemainingColorEntry
{
    public EBlockColorType BlockColorType;
    public Color Color;
}
