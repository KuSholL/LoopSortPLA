using System.Collections.Generic;

public sealed class CarrierUnloadPort
{
    private readonly CarrierBase _carrier;
    private readonly CarrierUnloadPayloadCollector _payloadCollector;

    public CarrierUnloadPort(CarrierBase carrier)
    {
        _carrier = carrier;
        _payloadCollector = new CarrierUnloadPayloadCollector(carrier);
    }

    public bool UnloadBlocks()
    {
        if (!_carrier.CanUnloadByMechanic()) return false;
        var topBlock = _carrier.BlockController.GetCurrentBlock();
        if (topBlock == null) return false;

        var capacity = CapacityManager.Instance;
        var maxCubeCount = capacity != null ? capacity.RemainingCubeCapacity : int.MaxValue;
        if (maxCubeCount <= 0) return false;

        var topRun = _carrier.BlockController.GetContiguousSameColorRun(topBlock);
        var linkedGroupId = GetFirstLinkGroupId(topRun);
        if (linkedGroupId >= 0)
        {
            return UnloadLinkedGroup(linkedGroupId, maxCubeCount, capacity);
        }

        CarrierUnloadRequest request;
        if (!_payloadCollector.TryCollect(maxCubeCount, out request)) return false;

        var delivery = ConveyorDeliverySystem.Instance;
        if (delivery == null || !delivery.TrySpawnCarrierUnload(request))
        {
            _carrier.RuntimeState.ClearDeliveryColor();
            return false;
        }

        if (capacity != null)
        {
            capacity.ReservePendingCubes(request.CubeCount);
        }
        RevealContainerKeys(request);
        _carrier.RuntimeState.MarkUnloading();
        if (GameEventBus.OnCarrierUnload != null)
        {
            GameEventBus.OnCarrierUnload();
        }
        return true;
    }

    private bool UnloadLinkedGroup(
        int linkGroupId,
        int remainingCapacity,
        CapacityManager capacity)
    {
        var linkSystem = BlockLinkSystem.Instance;
        List<BlockLinkSystem.CarrierUnloadGroup> groups;
        int totalCubeCount;
        bool isBlocked;
        if (linkSystem == null
            || !linkSystem.ResolveBlockLinkUnloadGroup(
                _carrier,
                out groups,
                out totalCubeCount,
                out isBlocked)
            || isBlocked
            || totalCubeCount > remainingCapacity)
        {
            PlayBlockedFeedback(linkGroupId);
            return false;
        }

        var requests = new List<CarrierUnloadRequest>();
        for (var i = 0; i < groups.Count; i++)
        {
            var group = groups[i];
            var collector = new CarrierUnloadPayloadCollector(group.Carrier);
            CarrierUnloadRequest request;
            var groupCapacity = GetGroupCubeCount(group.RunBlocks);
            if (!collector.TryCollect(groupCapacity, out request))
            {
                PlayBlockedFeedback(linkGroupId);
                return false;
            }
            requests.Add(request);
        }

        var delivery = ConveyorDeliverySystem.Instance;
        if (delivery == null) return false;

        for (var i = 0; i < requests.Count; i++)
        {
            var request = requests[i];
            if (!delivery.TrySpawnCarrierUnload(request))
            {
                request.SourceCarrier.RuntimeState.ClearDeliveryColor();
                return false;
            }

            if (capacity != null) capacity.ReservePendingCubes(request.CubeCount);
            RevealContainerKeys(request);
            request.SourceCarrier.RuntimeState.MarkUnloading();
            if (GameEventBus.OnCarrierUnload != null) GameEventBus.OnCarrierUnload();
        }

        return true;
    }

    private static int GetFirstLinkGroupId(List<Block> blocks)
    {
        if (blocks == null) return -1;
        for (var i = 0; i < blocks.Count; i++)
        {
            if (blocks[i] != null && blocks[i].HasLinkGroupId())
                return blocks[i].GetLinkGroupId();
        }
        return -1;
    }

    private static int GetGroupCubeCount(List<Block> blocks)
    {
        var count = 0;
        if (blocks == null) return count;
        for (var i = 0; i < blocks.Count; i++)
        {
            if (blocks[i] != null) count += blocks[i].GetExpectedUnloadCount();
        }
        return count;
    }

    private static void RevealContainerKeys(CarrierUnloadRequest request)
    {
        if (request == null || request.CubePayloads == null) return;
        var visited = new HashSet<Block>();
        for (var i = 0; i < request.CubePayloads.Count; i++)
        {
            var block = request.CubePayloads[i].SourceBlock;
            if (block != null && visited.Add(block))
                block.RevealContainerKeyIfNeeded();
        }
    }

    private static void PlayBlockedFeedback(int linkGroupId)
    {
        if (linkGroupId < 0 || BlockLinkSystem.Instance == null) return;
        var blocks = BlockLinkSystem.Instance.FindAllBlocksWithGroupId(linkGroupId);
        for (var i = 0; i < blocks.Count; i++)
        {
            var block = blocks[i];
            if (block == null) continue;
            var carrier = block.OwnerCarrier;
            if (carrier != null
                && carrier.LinkedBlockVisualController != null
                && carrier.LinkedBlockVisualController.TryPlayBlockedFullAnimation(block))
                continue;
            if (!block.IsLinkedVisualSuppressed()) block.PlayFullRevealAnimation();
        }
    }
}
