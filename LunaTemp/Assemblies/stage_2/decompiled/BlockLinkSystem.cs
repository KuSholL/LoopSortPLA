using System.Collections.Generic;

public sealed class BlockLinkSystem : MonoSingleton<BlockLinkSystem>
{
	public sealed class CarrierUnloadGroup
	{
		public CarrierBase Carrier;

		public List<Block> RunBlocks = new List<Block>();
	}

	public bool ResolveBlockLinkUnloadGroup(CarrierBase clickedCarrier, out List<CarrierUnloadGroup> unloadGroups, out int totalCubeCount, out bool isBlocked)
	{
		unloadGroups = new List<CarrierUnloadGroup>();
		totalCubeCount = 0;
		isBlocked = false;
		if (clickedCarrier == null || clickedCarrier.BlockController == null)
		{
			return false;
		}
		Block topBlock = clickedCarrier.BlockController.GetCurrentBlock();
		if (topBlock == null)
		{
			return false;
		}
		Queue<Block> queue = new Queue<Block>();
		HashSet<Block> visited = new HashSet<Block>();
		Dictionary<CarrierBase, HashSet<Block>> groupsByCarrier = new Dictionary<CarrierBase, HashSet<Block>>();
		AddRun(clickedCarrier, topBlock, queue, visited, groupsByCarrier);
		while (queue.Count > 0)
		{
			Block current = queue.Dequeue();
			if (current == null || !current.HasLinkGroupId())
			{
				continue;
			}
			List<Block> linkedBlocks = FindAllBlocksWithGroupId(current.GetLinkGroupId());
			for (int i = 0; i < linkedBlocks.Count; i++)
			{
				Block linkedBlock = linkedBlocks[i];
				if (!(linkedBlock == null) && !visited.Contains(linkedBlock))
				{
					CarrierBase linkedCarrier = linkedBlock.OwnerCarrier;
					if (linkedCarrier == null || !linkedBlock.CanBeginUnload())
					{
						isBlocked = true;
						return false;
					}
					AddRun(linkedCarrier, linkedBlock, queue, visited, groupsByCarrier);
				}
			}
		}
		foreach (KeyValuePair<CarrierBase, HashSet<Block>> pair in groupsByCarrier)
		{
			CarrierBase carrier = pair.Key;
			if (carrier == null || carrier.RuntimeState == null || !carrier.RuntimeState.IsIdle || !carrier.CanUnloadByMechanic() || (MonoSingleton<ConveyorDeliverySystem>.Instance != null && MonoSingleton<ConveyorDeliverySystem>.Instance.IsReceivingCube(carrier)))
			{
				isBlocked = true;
				return false;
			}
			List<Block> blocks = new List<Block>(pair.Value);
			blocks.Sort((Block a, Block b) => carrier.BlockController.GetBlockIndex(a).CompareTo(carrier.BlockController.GetBlockIndex(b)));
			Block currentTop = carrier.BlockController.GetCurrentBlock();
			if (blocks.Count == 0 || currentTop == null || blocks[0] != currentTop)
			{
				isBlocked = true;
				return false;
			}
			for (int j = 0; j < blocks.Count; j++)
			{
				if (!blocks[j].CanBeginUnload())
				{
					isBlocked = true;
					return false;
				}
				totalCubeCount += blocks[j].GetExpectedUnloadCount();
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
		List<Block> result = new List<Block>();
		if (!MonoSingleton<CarrierSystem>.HasInstance || MonoSingleton<CarrierSystem>.Instance.SpawnedCarriers == null)
		{
			return result;
		}
		IReadOnlyList<CarrierBase> carriers = MonoSingleton<CarrierSystem>.Instance.SpawnedCarriers;
		for (int carrierIndex = 0; carrierIndex < carriers.Count; carrierIndex++)
		{
			CarrierBase carrier = carriers[carrierIndex];
			if (carrier == null || carrier.BlockLayout == null)
			{
				continue;
			}
			for (int blockIndex = 0; blockIndex < carrier.MaxBlockCount; blockIndex++)
			{
				Block block = carrier.BlockLayout.GetBlockByIndex(blockIndex);
				if (block != null && block.HasContent && block.GetLinkGroupId() == groupId)
				{
					result.Add(block);
				}
			}
		}
		return result;
	}

	private static void AddRun(CarrierBase carrier, Block source, Queue<Block> queue, HashSet<Block> visited, Dictionary<CarrierBase, HashSet<Block>> groupsByCarrier)
	{
		List<Block> run = carrier.BlockController.GetContiguousSameColorRun(source);
		if (run == null)
		{
			return;
		}
		if (!groupsByCarrier.TryGetValue(carrier, out var carrierBlocks))
		{
			carrierBlocks = new HashSet<Block>();
			groupsByCarrier.Add(carrier, carrierBlocks);
		}
		for (int i = 0; i < run.Count; i++)
		{
			Block block = run[i];
			if (!(block == null))
			{
				carrierBlocks.Add(block);
				if (visited.Add(block))
				{
					queue.Enqueue(block);
				}
			}
		}
	}
}
