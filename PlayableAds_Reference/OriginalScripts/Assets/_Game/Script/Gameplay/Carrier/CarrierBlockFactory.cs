using System.Collections.Generic;

public class CarrierBlockFactory
{
    #region Fields

    private ColorConfigSO colorConfig;

    #endregion

    #region Constructor

    public CarrierBlockFactory(ColorConfigSO colorConfig)
    {
        this.colorConfig = colorConfig;
    }

    #endregion

    #region Build Blocks

    public void SetupCarrierBlocks(CarrierBase carrier, CarrierBlockLayoutBase layout, List<BlockData> blockDatas, bool suppressProgressAnimation = false)
    {
        if (layout == null) return;
        
        if (layout is CarrierBlockLayout multiLayout)
        {
            if (blockDatas == null || blockDatas.Count == 0)
            {
                multiLayout.RemoveExtraBlocks(0);
                return;
            }

            multiLayout.EnsureBlockSlots(blockDatas.Count);
            multiLayout.RemoveExtraBlocks(blockDatas.Count);
        }

        for (var i = 0; i < blockDatas.Count; i++)
        {
            var blockInstance = layout.GetBlockAt(i);
            if (blockInstance == null) continue;
            blockInstance.SetOwnerCarrier(carrier);
            ApplyBlockState(blockInstance, blockDatas[i], suppressProgressAnimation);
        }
    }

    #endregion

    #region Color Setup

    private void ApplyBlockState(Block block, BlockData blockData, bool suppressProgressAnimation)
    {
        if (block == null) return;
        if (IsEmptyBlock(blockData))
        {
            block.ClearContent();
            return;
        }
        
        block.ApplyBlockData(blockData, colorConfig, 1f, suppressProgressAnimation);
    }

    #endregion

    #region Validation

    private static bool IsEmptyBlock(BlockData blockData)
    {
        return blockData == null || blockData.BlockColor == EBlockColorType.None;
    }

    #endregion
}
