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
		if (!_carrier.CanUnloadByMechanic())
		{
			return false;
		}
		Block topBlock = _carrier.BlockController.GetCurrentBlock();
		if (topBlock == null)
		{
			return false;
		}
		CapacityManager capacity = MonoSingleton<CapacityManager>.Instance;
		int maxCubeCount = ((capacity != null) ? capacity.RemainingCubeCapacity : int.MaxValue);
		if (maxCubeCount <= 0)
		{
			return false;
		}
		List<Block> topRun = _carrier.BlockController.GetContiguousSameColorRun(topBlock);
		int linkedGroupId = GetFirstLinkGroupId(topRun);
		if (linkedGroupId >= 0)
		{
			return UnloadLinkedGroup(linkedGroupId, maxCubeCount, capacity);
		}
		if (!_payloadCollector.TryCollect(maxCubeCount, out var request))
		{
			return false;
		}
		ConveyorDeliverySystem delivery = MonoSingleton<ConveyorDeliverySystem>.Instance;
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

	private bool UnloadLinkedGroup(int linkGroupId, int remainingCapacity, CapacityManager capacity)
	{
		BlockLinkSystem linkSystem = MonoSingleton<BlockLinkSystem>.Instance;
		List<BlockLinkSystem.CarrierUnloadGroup> groups = default(List<BlockLinkSystem.CarrierUnloadGroup>);
		int totalCubeCount = default(int);
		bool isBlocked = default(bool);
		if (linkSystem == null || !linkSystem.ResolveBlockLinkUnloadGroup(_carrier, out groups, out totalCubeCount, out isBlocked) || isBlocked || totalCubeCount > remainingCapacity)
		{
			PlayBlockedFeedback(linkGroupId);
			return false;
		}
		List<CarrierUnloadRequest> requests = new List<CarrierUnloadRequest>();
		for (int j = 0; j < groups.Count; j++)
		{
			BlockLinkSystem.CarrierUnloadGroup group = groups[j];
			CarrierUnloadPayloadCollector collector = new CarrierUnloadPayloadCollector(group.Carrier);
			int groupCapacity = GetGroupCubeCount(group.RunBlocks);
			if (!collector.TryCollect(groupCapacity, out var request))
			{
				PlayBlockedFeedback(linkGroupId);
				return false;
			}
			requests.Add(request);
		}
		ConveyorDeliverySystem delivery = MonoSingleton<ConveyorDeliverySystem>.Instance;
		if (delivery == null)
		{
			return false;
		}
		for (int i = 0; i < requests.Count; i++)
		{
			CarrierUnloadRequest request2 = requests[i];
			if (!delivery.TrySpawnCarrierUnload(request2))
			{
				request2.SourceCarrier.RuntimeState.ClearDeliveryColor();
				return false;
			}
			if (capacity != null)
			{
				capacity.ReservePendingCubes(request2.CubeCount);
			}
			RevealContainerKeys(request2);
			request2.SourceCarrier.RuntimeState.MarkUnloading();
			if (GameEventBus.OnCarrierUnload != null)
			{
				GameEventBus.OnCarrierUnload();
			}
		}
		return true;
	}

	private static int GetFirstLinkGroupId(List<Block> blocks)
	{
		if (blocks == null)
		{
			return -1;
		}
		for (int i = 0; i < blocks.Count; i++)
		{
			if (blocks[i] != null && blocks[i].HasLinkGroupId())
			{
				return blocks[i].GetLinkGroupId();
			}
		}
		return -1;
	}

	private static int GetGroupCubeCount(List<Block> blocks)
	{
		int count = 0;
		if (blocks == null)
		{
			return count;
		}
		for (int i = 0; i < blocks.Count; i++)
		{
			if (blocks[i] != null)
			{
				count += blocks[i].GetExpectedUnloadCount();
			}
		}
		return count;
	}

	private static void RevealContainerKeys(CarrierUnloadRequest request)
	{
		if (request == null || request.CubePayloads == null)
		{
			return;
		}
		HashSet<Block> visited = new HashSet<Block>();
		for (int i = 0; i < request.CubePayloads.Count; i++)
		{
			Block block = request.CubePayloads[i].SourceBlock;
			if (block != null && visited.Add(block))
			{
				block.RevealContainerKeyIfNeeded();
			}
		}
	}

	private static void PlayBlockedFeedback(int linkGroupId)
	{
		if (linkGroupId < 0 || MonoSingleton<BlockLinkSystem>.Instance == null)
		{
			return;
		}
		List<Block> blocks = MonoSingleton<BlockLinkSystem>.Instance.FindAllBlocksWithGroupId(linkGroupId);
		for (int i = 0; i < blocks.Count; i++)
		{
			Block block = blocks[i];
			if (!(block == null))
			{
				CarrierBase carrier = block.OwnerCarrier;
				if ((!(carrier != null) || carrier.LinkedBlockVisualController == null || !carrier.LinkedBlockVisualController.TryPlayBlockedFullAnimation(block)) && !block.IsLinkedVisualSuppressed())
				{
					block.PlayFullRevealAnimation();
				}
			}
		}
	}
}
