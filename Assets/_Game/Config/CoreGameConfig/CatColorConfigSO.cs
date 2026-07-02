using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CatColorConfigSO", menuName = "ScriptableObjects/CatColorConfigSO")]
public class CatColorConfigSO : ScriptableObject
{
    public List<CatColorEntry> CatColorEntries;
}

[Serializable]
public class CatColorEntry
{
    public EBlockColorType BlockColorType;
    public Color Color;
} 
