using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CarrierLinkedBlockVisualConfigSO", menuName = "ScriptableObjects/Carrier/Linked Block Visual Config")]
public sealed class CarrierLinkedBlockVisualConfigSO : ScriptableObject
{
    [SerializeField] private List<LinkedBlockVisualEntry> linkedVisuals = new();

    public LinkedBlockVisualEntry GetEntry(int blockCount)
    {
        if (linkedVisuals == null) return null;

        for (var i = 0; i < linkedVisuals.Count; i++)
        {
            var entry = linkedVisuals[i];
            if (entry == null || entry.BlockCount != blockCount) continue;
            return entry;
        }

        return null;
    }
}

[Serializable]
public sealed class LinkedBlockVisualEntry
{
    [Min(2)] public int BlockCount = 2;
    public LinkedBlockVisual Prefab;
    public Vector3 LocalOffset;
}
