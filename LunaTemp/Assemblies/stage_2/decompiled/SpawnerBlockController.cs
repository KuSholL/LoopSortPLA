using System.Collections.Generic;

public sealed class SpawnerBlockController : CarrierBlockController
{
	private readonly Spawner _spawner;

	private bool _hasCollectedVisibleBlock;

	public SpawnerBlockController(Spawner spawner, CarrierBlockLayoutBase layout, CarrierBlockFactory factory, CarrierRuntimeState runtimeState, int maxBlockCount)
		: base(spawner, layout, factory, runtimeState, maxBlockCount)
	{
		_spawner = spawner;
	}

	public override void Reset()
	{
		base.Reset();
		_hasCollectedVisibleBlock = false;
	}

	public override Block GetCurrentBlock()
	{
		if (_hasCollectedVisibleBlock || _spawner.CurrentQueueIndex >= _spawner.BlocksQueue.Count)
		{
			return null;
		}
		return _spawner.SingleBlock;
	}

	public override bool TryGetCurrentMatchingBlock(EBlockColorType colorType, out Block block)
	{
		block = GetCurrentBlock();
		return block != null && block.GetBlockColorType() == colorType;
	}

	public override void CompleteCurrentBlock()
	{
		_hasCollectedVisibleBlock = true;
	}

	public override Block GetTopUnloadCandidateBlock()
	{
		Block block = GetCurrentBlock();
		return (block != null && block.CanBeginUnload()) ? block : null;
	}

	public override List<Block> GetPotentialUnloadBlocks(EBlockColorType colorType)
	{
		List<Block> result = new List<Block>();
		Block block = GetCurrentBlock();
		if (block != null && block.GetBlockColorType() == colorType && block.CanBeginUnload())
		{
			result.Add(block);
		}
		return result;
	}

	public override Block GetReceiveBlock(EBlockColorType blockColorType)
	{
		return null;
	}

	public override int GetBlockIndex(Block targetBlock)
	{
		return (!(targetBlock == _spawner.SingleBlock)) ? (-1) : 0;
	}

	public override int CleanupEmptyBlocks()
	{
		_hasCollectedVisibleBlock = false;
		Block block = _spawner.SingleBlock;
		if (block == null || !block.HasContent || block.GetCurrentCubes() != 0)
		{
			return -1;
		}
		block.ClearContent();
		_spawner.AdvanceQueue();
		if (_spawner.CurrentQueueIndex < _spawner.BlocksQueue.Count)
		{
			return 0;
		}
		_spawner.RuntimeState.MarkCompleted();
		return 1;
	}

	public override int RevealHiddenBlockAfterRelease(int releasedBlockIndex)
	{
		return -1;
	}

	public override void RefreshVisibleBlockVisuals()
	{
		_hasCollectedVisibleBlock = false;
		if (_spawner.SingleBlock != null && !_spawner.SingleBlock.HasContent)
		{
			_spawner.AdvanceQueue();
			if (_spawner.CurrentQueueIndex >= _spawner.BlocksQueue.Count)
			{
				_spawner.RuntimeState.MarkCompleted();
			}
		}
	}
}
