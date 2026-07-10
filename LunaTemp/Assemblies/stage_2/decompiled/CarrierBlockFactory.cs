using System.Collections.Generic;

public class CarrierBlockFactory
{
	private ColorConfigSO colorConfig;

	public CarrierBlockFactory(ColorConfigSO colorConfig)
	{
		this.colorConfig = colorConfig;
	}

	public void SetupCarrierBlocks(CarrierBase carrier, CarrierBlockLayoutBase layout, List<BlockData> blockDatas, bool suppressProgressAnimation = false)
	{
		if (layout == null)
		{
			return;
		}
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
		for (int i = 0; i < blockDatas.Count; i++)
		{
			Block blockInstance = layout.GetBlockAt(i);
			if (!(blockInstance == null))
			{
				blockInstance.SetOwnerCarrier(carrier);
				ApplyBlockState(blockInstance, blockDatas[i], suppressProgressAnimation);
			}
		}
	}

	private void ApplyBlockState(Block block, BlockData blockData, bool suppressProgressAnimation)
	{
		if (!(block == null))
		{
			if (IsEmptyBlock(blockData))
			{
				block.ClearContent();
			}
			else
			{
				block.ApplyBlockData(blockData, colorConfig, 1f, suppressProgressAnimation);
			}
		}
	}

	private static bool IsEmptyBlock(BlockData blockData)
	{
		return blockData == null || blockData.BlockColor == EBlockColorType.None;
	}
}
