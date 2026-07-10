using System.Collections.Generic;

public class CarrierBlockController
{
	private readonly CarrierBlockLayoutBase _layout;

	private readonly CarrierBlockFactory _factory;

	private readonly CarrierBase _carrier;

	private readonly CarrierRuntimeState _runtimeState;

	private readonly int _maxBlockCount;

	public CarrierBlockController(CarrierBase carrier, CarrierBlockLayoutBase layout, CarrierBlockFactory factory, CarrierRuntimeState runtimeState, int maxBlockCount)
	{
		_carrier = carrier;
		_layout = layout;
		_factory = factory;
		_runtimeState = runtimeState;
		_maxBlockCount = maxBlockCount;
	}

	public virtual void Reset()
	{
	}

	public void BuildBlocks(List<BlockData> blockDatas, bool suppressProgressAnimation = false)
	{
		if (!(_layout == null))
		{
			List<BlockData> normalizedData = NormalizeRuntimeBlockData(blockDatas);
			_factory?.SetupCarrierBlocks(_carrier, _layout, normalizedData, suppressProgressAnimation);
			if (_layout is CarrierBlockLayout multiLayout)
			{
				multiLayout.ArrangeBlocks();
			}
		}
	}

	private List<BlockData> NormalizeRuntimeBlockData(List<BlockData> sourceBlocks)
	{
		List<BlockData> normalizedBlocks = new List<BlockData>(_maxBlockCount);
		for (int i = 0; i < _maxBlockCount; i++)
		{
			if (sourceBlocks != null && i < sourceBlocks.Count && sourceBlocks[i] != null)
			{
				normalizedBlocks.Add(sourceBlocks[i]);
			}
			else
			{
				normalizedBlocks.Add(CreateEmptyRuntimeBlock());
			}
		}
		return normalizedBlocks;
	}

	private static BlockData CreateEmptyRuntimeBlock()
	{
		return new BlockData
		{
			BlockColor = EBlockColorType.None,
			Mechanics = new List<BlockMechanicData>()
		};
	}

	public virtual Block GetCurrentBlock()
	{
		if (_layout == null || _runtimeState.CurrentBlockIndex >= _maxBlockCount)
		{
			return null;
		}
		int blockIndex = FindNextActiveBlockIndex(_runtimeState.CurrentBlockIndex);
		if (blockIndex >= _maxBlockCount)
		{
			_runtimeState.SetCurrentBlockIndex(blockIndex);
			return null;
		}
		_runtimeState.SetCurrentBlockIndex(blockIndex);
		Block block = _layout.GetBlockByIndex(blockIndex);
		return (block != null && block.HasContent) ? block : null;
	}

	public virtual bool TryGetCurrentMatchingBlock(EBlockColorType colorType, out Block block)
	{
		block = GetCurrentBlock();
		return block != null && block.GetBlockColorType() == colorType;
	}

	public virtual void CompleteCurrentBlock()
	{
		AdvanceCurrentBlockIndex();
	}

	public virtual Block GetTopUnloadCandidateBlock()
	{
		if (_layout == null || _runtimeState.CurrentBlockIndex >= _maxBlockCount)
		{
			return null;
		}
		int firstBlockIndex = FindNextActiveBlockIndex(_runtimeState.CurrentBlockIndex);
		if (firstBlockIndex >= _maxBlockCount)
		{
			return null;
		}
		Block firstBlock = _layout.GetBlockByIndex(firstBlockIndex);
		return (firstBlock != null && firstBlock.CanBeginUnload()) ? firstBlock : null;
	}

	public virtual List<Block> GetPotentialUnloadBlocks(EBlockColorType colorType)
	{
		List<Block> result = new List<Block>();
		if (_layout == null || _runtimeState.CurrentBlockIndex >= _maxBlockCount)
		{
			return result;
		}
		int firstBlockIndex = FindNextActiveBlockIndex(_runtimeState.CurrentBlockIndex);
		if (firstBlockIndex >= _maxBlockCount)
		{
			return result;
		}
		for (int i = firstBlockIndex; i < _maxBlockCount; i++)
		{
			Block block = _layout.GetBlockByIndex(i);
			if (block == null || !block.HasContent || block.GetBlockColorType() != colorType || !block.CanBeginUnload())
			{
				break;
			}
			result.Add(block);
		}
		return result;
	}

	public List<Block> GetContiguousSameColorRun(Block startBlock)
	{
		List<Block> run = new List<Block>();
		if (startBlock == null || !startBlock.HasContent)
		{
			return run;
		}
		int startIdx = GetBlockIndex(startBlock);
		if (startIdx < 0)
		{
			return run;
		}
		EBlockColorType colorType = startBlock.GetBlockColorType();
		int j = startIdx;
		while (j >= 0)
		{
			Block block2 = _layout.GetBlockByIndex(j);
			if (block2 != null && block2.HasContent && block2.GetBlockColorType() == colorType && block2.CanBeginUnload())
			{
				run.Add(block2);
				j--;
				continue;
			}
			break;
		}
		for (int i = startIdx + 1; i < _maxBlockCount; i++)
		{
			Block block = _layout.GetBlockByIndex(i);
			if (block != null && block.HasContent && block.GetBlockColorType() == colorType && block.CanBeginUnload())
			{
				run.Add(block);
				continue;
			}
			break;
		}
		run.Sort((Block a, Block b) => GetBlockIndex(a).CompareTo(GetBlockIndex(b)));
		return run;
	}

	public virtual Block GetReceiveBlock(EBlockColorType blockColorType)
	{
		Block fallbackBlock = null;
		for (int i = 0; i < _maxBlockCount; i++)
		{
			Block block = _layout?.GetBlockByIndex(i);
			if (block == null)
			{
				continue;
			}
			if (!block.HasContent)
			{
				fallbackBlock = block;
				continue;
			}
			if (block.GetBlockColorType() != blockColorType)
			{
				return null;
			}
			if (block.IsAvailableForReceive(blockColorType))
			{
				return block;
			}
			return fallbackBlock;
		}
		return fallbackBlock;
	}

	public virtual int GetBlockIndex(Block targetBlock)
	{
		if (_layout == null || targetBlock == null)
		{
			return -1;
		}
		for (int i = 0; i < _maxBlockCount; i++)
		{
			if (_layout.GetBlockByIndex(i) == targetBlock)
			{
				return i;
			}
		}
		return -1;
	}

	public virtual void RefreshVisibleBlockVisuals()
	{
	}

	public void Wake(int blockIndex)
	{
		_runtimeState.WakeAtBlock(blockIndex);
	}

	private void AdvanceCurrentBlockIndex()
	{
		int nextIndex;
		for (nextIndex = _runtimeState.CurrentBlockIndex + 1; nextIndex < _maxBlockCount; nextIndex++)
		{
			Block block = _layout.GetBlockByIndex(nextIndex);
			if (block != null && block.HasContent && !block.IsOpened)
			{
				_runtimeState.SetCurrentBlockIndex(nextIndex);
				return;
			}
		}
		_runtimeState.SetCurrentBlockIndex(nextIndex);
		_runtimeState.MarkCompleted();
	}

	public virtual int CleanupEmptyBlocks()
	{
		if (_layout == null)
		{
			return -1;
		}
		int revealedHiddenBlockIndex = -1;
		for (int i = 0; i < _maxBlockCount; i++)
		{
			Block block = _layout.GetBlockByIndex(i);
			if (block != null && block.HasContent && block.IsOpened && block.GetCurrentCubes() == 0)
			{
				block.ClearContent();
				if (revealedHiddenBlockIndex < 0)
				{
					revealedHiddenBlockIndex = RevealNextHiddenBlock(i);
				}
			}
		}
		return revealedHiddenBlockIndex;
	}

	public virtual int RevealHiddenBlockAfterRelease(int releasedBlockIndex)
	{
		if (releasedBlockIndex < 0 || releasedBlockIndex >= _maxBlockCount)
		{
			return -1;
		}
		int revealedBlockIndex = RevealNextHiddenBlock(releasedBlockIndex);
		if (revealedBlockIndex < 0)
		{
			return -1;
		}
		return revealedBlockIndex;
	}

	private int FindNextActiveBlockIndex(int startIndex)
	{
		int nextIndex;
		for (nextIndex = startIndex; nextIndex < _maxBlockCount; nextIndex++)
		{
			Block block = _layout.GetBlockByIndex(nextIndex);
			if (block != null && block.HasContent)
			{
				return nextIndex;
			}
		}
		return nextIndex;
	}

	private int RevealNextHiddenBlock(int releasedBlockIndex)
	{
		int nextIndex = FindNextActiveBlockIndex(releasedBlockIndex + 1);
		if (nextIndex >= _maxBlockCount)
		{
			return -1;
		}
		Block nextBlock = _layout.GetBlockByIndex(nextIndex);
		if (nextBlock == null)
		{
			return -1;
		}
		bool hadHiddenVisual = nextBlock.IsHiddenVisualActive();
		nextBlock.NotifyMechanicEvent(new PreviousBlockReleasedEvent(), true);
		return hadHiddenVisual ? nextIndex : (-1);
	}
}
