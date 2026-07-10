using System.Collections.Generic;
using UnityEngine;

public sealed class BlockLinkSystem : MonoSingleton<BlockLinkSystem>
{
    public sealed class CarrierUnloadGroup
    {
        public CarrierBase Carrier;
        public List<Block> RunBlocks = new List<Block>();
    }

    public bool ResolveBlockLinkUnloadGroup(
        CarrierBase clickedCarrier,
        out List<CarrierUnloadGroup> unloadGroups,
        out int totalCubeCount,
        out bool isBlocked)
    {
        unloadGroups = new List<CarrierUnloadGroup>();
        totalCubeCount = 0;
        isBlocked = false;
        if (clickedCarrier == null || clickedCarrier.BlockController == null) return false;

        var topBlock = clickedCarrier.BlockController.GetCurrentBlock();
        if (topBlock == null) return false;

        var queue = new Queue<Block>();
        var visited = new HashSet<Block>();
        var groupsByCarrier = new Dictionary<CarrierBase, HashSet<Block>>();
        AddRun(clickedCarrier, topBlock, queue, visited, groupsByCarrier);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current == null || !current.HasLinkGroupId()) continue;

            var linkedBlocks = FindAllBlocksWithGroupId(current.GetLinkGroupId());
            for (var i = 0; i < linkedBlocks.Count; i++)
            {
                var linkedBlock = linkedBlocks[i];
                if (linkedBlock == null || visited.Contains(linkedBlock)) continue;

                var linkedCarrier = linkedBlock.OwnerCarrier;
                if (linkedCarrier == null || !linkedBlock.CanBeginUnload())
                {
                    isBlocked = true;
                    return false;
                }

                AddRun(linkedCarrier, linkedBlock, queue, visited, groupsByCarrier);
            }
        }

        foreach (var pair in groupsByCarrier)
        {
            var carrier = pair.Key;
            if (carrier == null
                || carrier.RuntimeState == null
                || !carrier.RuntimeState.IsIdle
                || !carrier.CanUnloadByMechanic()
                || (ConveyorDeliverySystem.Instance != null
                    && ConveyorDeliverySystem.Instance.IsReceivingCube(carrier)))
            {
                isBlocked = true;
                return false;
            }

            var blocks = new List<Block>(pair.Value);
            blocks.Sort((a, b) =>
                carrier.BlockController.GetBlockIndex(a)
                    .CompareTo(carrier.BlockController.GetBlockIndex(b)));

            var currentTop = carrier.BlockController.GetCurrentBlock();
            if (blocks.Count == 0 || currentTop == null || blocks[0] != currentTop)
            {
                isBlocked = true;
                return false;
            }

            for (var i = 0; i < blocks.Count; i++)
            {
                if (!blocks[i].CanBeginUnload())
                {
                    isBlocked = true;
                    return false;
                }
                totalCubeCount += blocks[i].GetExpectedUnloadCount();
            }

            unloadGroups.Add(new CarrierUnloadGroup
            {
                Carrier = carrier,
                RunBlocks = blocks
            });
        }

        return unloadGroups.Count > 0;
    }

    public List<Block> FindAllBlocksWithGroupId(int groupId)
    {
        var result = new List<Block>();
        if (!CarrierSystem.HasInstance || CarrierSystem.Instance.SpawnedCarriers == null) return result;

        var carriers = CarrierSystem.Instance.SpawnedCarriers;
        for (var carrierIndex = 0; carrierIndex < carriers.Count; carrierIndex++)
        {
            var carrier = carriers[carrierIndex];
            if (carrier == null || carrier.BlockLayout == null) continue;

            for (var blockIndex = 0; blockIndex < carrier.MaxBlockCount; blockIndex++)
            {
                var block = carrier.BlockLayout.GetBlockByIndex(blockIndex);
                if (block != null
                    && block.HasContent
                    && block.GetLinkGroupId() == groupId)
                {
                    result.Add(block);
                }
            }
        }

        return result;
    }

    private static void AddRun(
        CarrierBase carrier,
        Block source,
        Queue<Block> queue,
        HashSet<Block> visited,
        Dictionary<CarrierBase, HashSet<Block>> groupsByCarrier)
    {
        var run = carrier.BlockController.GetContiguousSameColorRun(source);
        if (run == null) return;

        HashSet<Block> carrierBlocks;
        if (!groupsByCarrier.TryGetValue(carrier, out carrierBlocks))
        {
            carrierBlocks = new HashSet<Block>();
            groupsByCarrier.Add(carrier, carrierBlocks);
        }

        for (var i = 0; i < run.Count; i++)
        {
            var block = run[i];
            if (block == null) continue;
            carrierBlocks.Add(block);
            if (visited.Add(block)) queue.Enqueue(block);
        }
    }
}
