using System.Collections.Generic;
using UnityEngine;

public class BlockLinkVisualManager : MonoSingleton<BlockLinkVisualManager>
{
    [Header("Visual Settings")]
    [SerializeField] private BlockLinkVisual linkVisualPrefab; // Prefab LineRenderer/BlockLinkVisual

    private readonly List<BlockLinkVisual> _activeVisuals = new();

    private void OnEnable()
    {
        GameEventBus.OnInitLoadLevel += ClearAllVisuals;
        GameEventBus.OnCarrierUnload += RefreshAllVisualPositions;
        GameEventBus.OnCarrierUnloadDone += RefreshAllVisualPositions;
        GameEventBus.OnCarrierPickupDone += RefreshAllVisualPositions;
        GameEventBus.OnUndoSuccess += RefreshAllVisualPositions;
        GameEventBus.OnCarrierFinished += OnCarrierFinished;
    }

    private void OnDisable()
    {
        GameEventBus.OnInitLoadLevel -= ClearAllVisuals;
        GameEventBus.OnCarrierUnload -= RefreshAllVisualPositions;
        GameEventBus.OnCarrierUnloadDone -= RefreshAllVisualPositions;
        GameEventBus.OnCarrierPickupDone -= RefreshAllVisualPositions;
        GameEventBus.OnUndoSuccess -= RefreshAllVisualPositions;
        GameEventBus.OnCarrierFinished -= OnCarrierFinished;
        ClearAllVisuals();
    }

    private void OnCarrierFinished(EBlockColorType colorType)
    {
        RefreshAllVisualPositions();
    }

    public void SetupLevelLinks()
    {
        ClearAllVisuals();

        if (CarrierSystem.Instance == null || CarrierSystem.Instance.SpawnedCarriers == null) return;

        // Gom nhóm các block theo Group ID, đảm bảo tối đa 1 block đại diện cho mỗi carrier trong mỗi nhóm
        var linkGroups = new Dictionary<int, List<(CarrierBase carrier, Block block)>>();

        foreach (var carrier in CarrierSystem.Instance.SpawnedCarriers)
        {
            if (carrier == null || carrier.BlockLayout == null) continue;

            var bestBlockPerGroup = new Dictionary<int, Block>();
            for (int i = 0; i < carrier.MaxBlockCount; i++)
            {
                var block = carrier.BlockLayout.GetBlockByIndex(i);
                if (block != null && block.HasContent && block.HasLinkGroupId())
                {
                    int groupId = block.GetLinkGroupId();
                    if (!bestBlockPerGroup.ContainsKey(groupId))
                    {
                        bestBlockPerGroup[groupId] = block;
                    }
                }
            }

            foreach (var kvp in bestBlockPerGroup)
            {
                int groupId = kvp.Key;
                Block block = kvp.Value;
                if (!linkGroups.ContainsKey(groupId))
                {
                    linkGroups[groupId] = new List<(CarrierBase, Block)>();
                }
                linkGroups[groupId].Add((carrier, block));
            }
        }

        // Tạo visual kết nối giữa các block trong cùng một nhóm bằng thuật toán Prim MST
        foreach (var pair in linkGroups)
        {
            var members = pair.Value;
            if (members.Count < 2) continue;

            int n = members.Count;
            var inMST = new bool[n];
            var minEdgeDist = new float[n];
            var parent = new int[n];

            for (int i = 0; i < n; i++)
            {
                minEdgeDist[i] = float.MaxValue;
                parent[i] = -1;
            }

            minEdgeDist[0] = 0f;

            for (int step = 0; step < n; step++)
            {
                int u = -1;
                float minDist = float.MaxValue;
                for (int i = 0; i < n; i++)
                {
                    if (!inMST[i] && minEdgeDist[i] < minDist)
                    {
                        minDist = minEdgeDist[i];
                        u = i;
                    }
                }

                if (u == -1) break;

                inMST[u] = true;

                if (parent[u] != -1)
                {
                    var parentMember = members[parent[u]];
                    var currentMember = members[u];
                    CreateVisualLink(parentMember.carrier, parentMember.block, currentMember.carrier, currentMember.block);
                }

                Vector3 posU = GetBlockCenterPosition(members[u].carrier, members[u].block);
                for (int v = 0; v < n; v++)
                {
                    if (!inMST[v])
                    {
                        Vector3 posV = GetBlockCenterPosition(members[v].carrier, members[v].block);
                        float dist = Vector3.Distance(posU, posV);
                        if (dist < minEdgeDist[v])
                        {
                            minEdgeDist[v] = dist;
                            parent[v] = u;
                        }
                    }
                }
            }
        }
    }

    private void CreateVisualLink(CarrierBase carrierA, Block blockA, CarrierBase carrierB, Block blockB)
    {
        BlockLinkVisual linkVisual = null;

        if (linkVisualPrefab != null)
        {
            if (Application.isPlaying && PoolManagerNew.Instance != null)
            {
                linkVisual = PoolManagerNew.Instance.PopFromPool(linkVisualPrefab, transform);
            }
            else
            {
                var go = Instantiate(linkVisualPrefab.gameObject, transform);
                linkVisual = go.GetComponent<BlockLinkVisual>();
                if (linkVisual == null)
                {
                    linkVisual = go.AddComponent<BlockLinkVisual>();
                }
            }
        }
        else
        {
            var go = new GameObject($"Link_{carrierA.name}_{carrierB.name}");
            go.transform.SetParent(transform);
            linkVisual = go.AddComponent<BlockLinkVisual>();
        }

        if (linkVisual != null)
        {
            linkVisual.Setup(carrierA, blockA, carrierB, blockB);
            _activeVisuals.Add(linkVisual);
        }
    }

    public void RefreshAllVisualPositions()
    {
        for (int i = _activeVisuals.Count - 1; i >= 0; i--)
        {
            var visual = _activeVisuals[i];
            if (visual == null)
            {
                _activeVisuals.RemoveAt(i);
                continue;
            }
            visual.UpdatePositions();
        }
    }

    public void ClearAllVisuals()
    {
        foreach (var visual in _activeVisuals)
        {
            if (visual != null)
            {
                if (Application.isPlaying && PoolManagerNew.Instance != null)
                {
                    PoolManagerNew.Instance.PushToPool(visual);
                }
                else
                {
                    Destroy(visual.gameObject);
                }
            }
        }
        _activeVisuals.Clear();
    }

    private Vector3 GetBlockCenterPosition(CarrierBase carrier, Block block)
    {
        if (carrier == null || block == null) return Vector3.zero;

        // Nếu block đang thuộc về một nhóm gộp visual (mesh2, mesh3, mesh4)
        if (carrier.LinkedBlockVisualController != null)
        {
            var linkedVisual = carrier.LinkedBlockVisualController.GetLinkedVisualContainingBlock(block);
            if (linkedVisual != null)
            {
                return linkedVisual.transform.position;
            }
        }

        return block.AnimationPivot != null ? block.AnimationPivot.position : block.transform.position;
    }
}
