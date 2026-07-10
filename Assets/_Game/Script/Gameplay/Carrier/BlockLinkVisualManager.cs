using System.Collections.Generic;
using UnityEngine;

public sealed class BlockLinkVisualManager : MonoSingleton<BlockLinkVisualManager>
{
    [SerializeField] private BlockLinkVisual linkVisualPrefab;
    private readonly List<BlockLinkVisual> _activeVisuals = new List<BlockLinkVisual>();

    protected override void Awake()
    {
        base.Awake();
        if (linkVisualPrefab == null && ConfigManager.Instance != null)
        {
            var config = ConfigManager.Instance.GetCarrierConfig();
            if (config != null) linkVisualPrefab = config.BlockLinkVisualPrefab;
        }
    }

    private void OnEnable()
    {
        GameEventBus.OnInitLoadLevel += ClearAllVisuals;
        GameEventBus.OnCarrierUnload += RefreshAllVisualPositions;
        GameEventBus.OnCarrierUnloadDone += RefreshAllVisualPositions;
        GameEventBus.OnCarrierPickupDone += RefreshAllVisualPositions;
        GameEventBus.OnCarrierFinished += OnCarrierFinished;
    }

    private void OnDisable()
    {
        GameEventBus.OnInitLoadLevel -= ClearAllVisuals;
        GameEventBus.OnCarrierUnload -= RefreshAllVisualPositions;
        GameEventBus.OnCarrierUnloadDone -= RefreshAllVisualPositions;
        GameEventBus.OnCarrierPickupDone -= RefreshAllVisualPositions;
        GameEventBus.OnCarrierFinished -= OnCarrierFinished;
        ClearAllVisuals();
    }

    public void SetupLevelLinks()
    {
        ClearAllVisuals();
        if (!CarrierSystem.HasInstance || CarrierSystem.Instance.SpawnedCarriers == null) return;

        var groups = new Dictionary<int, List<LinkMember>>();
        var carriers = CarrierSystem.Instance.SpawnedCarriers;
        for (var carrierIndex = 0; carrierIndex < carriers.Count; carrierIndex++)
        {
            var carrier = carriers[carrierIndex];
            if (carrier == null || carrier.BlockLayout == null) continue;
            var seenGroups = new HashSet<int>();

            for (var blockIndex = 0; blockIndex < carrier.MaxBlockCount; blockIndex++)
            {
                var block = carrier.BlockLayout.GetBlockByIndex(blockIndex);
                if (block == null || !block.HasContent || !block.HasLinkGroupId()) continue;
                var groupId = block.GetLinkGroupId();
                if (!seenGroups.Add(groupId)) continue;

                List<LinkMember> members;
                if (!groups.TryGetValue(groupId, out members))
                {
                    members = new List<LinkMember>();
                    groups.Add(groupId, members);
                }
                members.Add(new LinkMember(carrier, block));
            }
        }

        foreach (var pair in groups)
        {
            CreateMinimumSpanningTree(pair.Value);
        }
    }

    public void RefreshAllVisualPositions()
    {
        for (var i = _activeVisuals.Count - 1; i >= 0; i--)
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
        for (var i = 0; i < _activeVisuals.Count; i++)
        {
            var visual = _activeVisuals[i];
            if (visual == null) continue;
            if (PoolManagerNew.Instance != null)
                PoolManagerNew.Instance.PushToPool(visual);
            else
                Destroy(visual.gameObject);
        }
        _activeVisuals.Clear();
    }

    private void CreateMinimumSpanningTree(List<LinkMember> members)
    {
        if (members == null || members.Count < 2 || linkVisualPrefab == null) return;
        var count = members.Count;
        var included = new bool[count];
        var bestDistance = new float[count];
        var parent = new int[count];

        for (var i = 0; i < count; i++)
        {
            bestDistance[i] = float.MaxValue;
            parent[i] = -1;
        }
        bestDistance[0] = 0f;

        for (var step = 0; step < count; step++)
        {
            var next = -1;
            var nearest = float.MaxValue;
            for (var i = 0; i < count; i++)
            {
                if (included[i] || bestDistance[i] >= nearest) continue;
                nearest = bestDistance[i];
                next = i;
            }
            if (next < 0) break;

            included[next] = true;
            if (parent[next] >= 0)
                CreateVisual(members[parent[next]], members[next]);

            var nextPosition = GetCenter(members[next]);
            for (var i = 0; i < count; i++)
            {
                if (included[i]) continue;
                var distance = Vector3.SqrMagnitude(nextPosition - GetCenter(members[i]));
                if (distance >= bestDistance[i]) continue;
                bestDistance[i] = distance;
                parent[i] = next;
            }
        }
    }

    private void CreateVisual(LinkMember first, LinkMember second)
    {
        var visual = PoolManagerNew.Instance != null
            ? PoolManagerNew.Instance.PopFromPool(linkVisualPrefab, transform)
            : Instantiate(linkVisualPrefab, transform);
        if (visual == null) return;
        LunaMaterialUtility.NormalizeRenderers(visual.gameObject);
        visual.Setup(first.Carrier, first.Block, second.Carrier, second.Block);
        _activeVisuals.Add(visual);
    }

    private static Vector3 GetCenter(LinkMember member)
    {
        return member.Block.AnimationPivot != null
            ? member.Block.AnimationPivot.position
            : member.Block.transform.position;
    }

    private void OnCarrierFinished(EBlockColorType colorType)
    {
        RefreshAllVisualPositions();
    }

    private struct LinkMember
    {
        public readonly CarrierBase Carrier;
        public readonly Block Block;

        public LinkMember(CarrierBase carrier, Block block)
        {
            Carrier = carrier;
            Block = block;
        }
    }
}
