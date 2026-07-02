using System.Collections.Generic;

public class CarrierBlockController
{
    #region Fields

    private readonly CarrierBlockLayoutBase _layout;
    private readonly CarrierBlockFactory _factory;
    private readonly CarrierBase _carrier;
    private readonly CarrierRuntimeState _runtimeState;
    private readonly int _maxBlockCount;

    #endregion

    #region Constructor

    public CarrierBlockController(
        CarrierBase carrier,
        CarrierBlockLayoutBase layout,
        CarrierBlockFactory factory,
        CarrierRuntimeState runtimeState,
        int maxBlockCount)
    {
        _carrier = carrier;
        _layout = layout;
        _factory = factory;
        _runtimeState = runtimeState;
        _maxBlockCount = maxBlockCount;
    }

    #endregion

    public virtual void Reset() {}

    #region Build Flow

    public void BuildBlocks(List<BlockData> blockDatas, bool suppressProgressAnimation = false)
    {
        if (_layout == null) return;

        var normalizedData = NormalizeRuntimeBlockData(blockDatas);

        _factory?.SetupCarrierBlocks(_carrier, _layout, normalizedData, suppressProgressAnimation);
        if (_layout is CarrierBlockLayout multiLayout)
        {
            multiLayout.ArrangeBlocks();
        }
    }

    #endregion

    #region Runtime Block Data

    private List<BlockData> NormalizeRuntimeBlockData(List<BlockData> sourceBlocks)
    {
        var normalizedBlocks = new List<BlockData>(_maxBlockCount);
        for (var i = 0; i < _maxBlockCount; i++)
        {
            if (sourceBlocks != null && i < sourceBlocks.Count && sourceBlocks[i] != null)
            {
                normalizedBlocks.Add(sourceBlocks[i]);
                continue;
            }

            normalizedBlocks.Add(CreateEmptyRuntimeBlock());
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

    #endregion

    #region Current Block

    public virtual Block GetCurrentBlock()
    {
        if (_layout == null || _runtimeState.CurrentBlockIndex >= _maxBlockCount) return null;
        var blockIndex = FindNextActiveBlockIndex(_runtimeState.CurrentBlockIndex);
        if (blockIndex >= _maxBlockCount)
        {
            _runtimeState.SetCurrentBlockIndex(blockIndex);
            return null;
        }

        _runtimeState.SetCurrentBlockIndex(blockIndex);
        var block = _layout.GetBlockByIndex(blockIndex);
        return block != null && block.HasContent ? block : null;
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
        if (_layout == null || _runtimeState.CurrentBlockIndex >= _maxBlockCount) return null;

        var firstBlockIndex = FindNextActiveBlockIndex(_runtimeState.CurrentBlockIndex);
        if (firstBlockIndex >= _maxBlockCount) return null;

        var firstBlock = _layout.GetBlockByIndex(firstBlockIndex);
        return firstBlock != null && firstBlock.CanBeginUnload() ? firstBlock : null;
    }

    public virtual List<Block> GetPotentialUnloadBlocks(EBlockColorType colorType)
    {
        var result = new List<Block>();
        if (_layout == null || _runtimeState.CurrentBlockIndex >= _maxBlockCount) return result;

        var firstBlockIndex = FindNextActiveBlockIndex(_runtimeState.CurrentBlockIndex);
        if (firstBlockIndex >= _maxBlockCount) return result;

        for (var i = firstBlockIndex; i < _maxBlockCount; i++)
        {
            var block = _layout.GetBlockByIndex(i);
            if (block == null || !block.HasContent) break;
            if (block.GetBlockColorType() != colorType) break;
            if (!block.CanBeginUnload()) break;

            result.Add(block);
        }

        return result;
    }

    public List<Block> GetContiguousSameColorRun(Block startBlock)
    {
        var run = new List<Block>();
        if (startBlock == null || !startBlock.HasContent) return run;

        var startIdx = GetBlockIndex(startBlock);
        if (startIdx < 0) return run;

        var colorType = startBlock.GetBlockColorType();

        // Quét ngược xuống đáy (chỉ số index giảm dần)
        for (int i = startIdx; i >= 0; i--)
        {
            var block = _layout.GetBlockByIndex(i);
            if (block != null && block.HasContent && block.GetBlockColorType() == colorType && block.CanBeginUnload())
                run.Add(block);
            else
                break;
        }

        // Quét xuôi lên đỉnh (chỉ số index tăng dần)
        for (int i = startIdx + 1; i < _maxBlockCount; i++)
        {
            var block = _layout.GetBlockByIndex(i);
            if (block != null && block.HasContent && block.GetBlockColorType() == colorType && block.CanBeginUnload())
                run.Add(block);
            else
                break;
        }

        // Sắp xếp lại danh sách theo thứ tự từ đỉnh xuống đáy (chỉ số index tăng dần) để phục vụ unload chuẩn
        run.Sort((a, b) => GetBlockIndex(a).CompareTo(GetBlockIndex(b)));
        return run;
    }

    #endregion

    #region Receive Query

    public virtual Block GetReceiveBlock(EBlockColorType blockColorType)
    {
        Block fallbackBlock = null;
        for (var i = 0; i < _maxBlockCount; i++)
        {
            var block = _layout?.GetBlockByIndex(i);
            if (block == null) continue;
            if (!block.HasContent)
            {
                fallbackBlock = block;
                continue;
            }

            if (block.GetBlockColorType() != blockColorType) return null;
            if (block.IsAvailableForReceive(blockColorType)) return block;
            return fallbackBlock;
        }

        return fallbackBlock;
    }

    public virtual int GetBlockIndex(Block targetBlock)
    {
        if (_layout == null || targetBlock == null) return -1;

        for (var i = 0; i < _maxBlockCount; i++)
            if (_layout.GetBlockByIndex(i) == targetBlock)
                return i;

        return -1;
    }

    #endregion

    #region Visuals

    public virtual void RefreshVisibleBlockVisuals() {}

    #endregion

    #region Wake

    public void Wake(int blockIndex)
    {
        _runtimeState.WakeAtBlock(blockIndex);
    }

    #endregion

    #region Block Progress

    private void AdvanceCurrentBlockIndex()
    {
        var nextIndex = _runtimeState.CurrentBlockIndex + 1;
        while (nextIndex < _maxBlockCount)
        {
            var block = _layout.GetBlockByIndex(nextIndex);
            if (block != null && block.HasContent && !block.IsOpened)
            {
                _runtimeState.SetCurrentBlockIndex(nextIndex);
                return;
            }

            nextIndex++;
        }

        _runtimeState.SetCurrentBlockIndex(nextIndex);
        _runtimeState.MarkCompleted();
    }

    public virtual int CleanupEmptyBlocks()
    {
        if (_layout == null) return -1;
        var revealedHiddenBlockIndex = -1;
        for (var i = 0; i < _maxBlockCount; i++)
        {
            var block = _layout.GetBlockByIndex(i);
            if (block != null && block.HasContent && block.IsOpened && block.GetCurrentCubes() == 0)
            {
                block.ClearContent();
                if (revealedHiddenBlockIndex < 0)
                    revealedHiddenBlockIndex = RevealNextHiddenBlock(i);
            }
        }

        return revealedHiddenBlockIndex;
    }

    public virtual int RevealHiddenBlockAfterRelease(int releasedBlockIndex)
    {
        if (releasedBlockIndex < 0 || releasedBlockIndex >= _maxBlockCount) return -1;
        var revealedBlockIndex = RevealNextHiddenBlock(releasedBlockIndex);
        if (revealedBlockIndex < 0) return -1;
        return revealedBlockIndex;
    }
    
    private int FindNextActiveBlockIndex(int startIndex)
    {
        var nextIndex = startIndex;
        while (nextIndex < _maxBlockCount)
        {
            var block = _layout.GetBlockByIndex(nextIndex);
            if (block != null && block.HasContent) return nextIndex;
            nextIndex++;
        }

        return nextIndex;
    }

    private int RevealNextHiddenBlock(int releasedBlockIndex)
    {
        var nextIndex = FindNextActiveBlockIndex(releasedBlockIndex + 1);
        if (nextIndex >= _maxBlockCount) return -1;

        var nextBlock = _layout.GetBlockByIndex(nextIndex);
        if (nextBlock == null) return -1;

        var hadHiddenVisual = nextBlock.IsHiddenVisualActive();
        nextBlock.NotifyMechanicEvent(new PreviousBlockReleasedEvent(), true);
        return hadHiddenVisual ? nextIndex : -1;
    }

    #endregion
}
