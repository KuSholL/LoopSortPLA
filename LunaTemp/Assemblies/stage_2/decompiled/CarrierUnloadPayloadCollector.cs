using System.Collections.Generic;

public sealed class CarrierUnloadPayloadCollector
{
	private readonly CarrierBase _carrier;

	public CarrierUnloadPayloadCollector(CarrierBase carrier)
	{
		_carrier = carrier;
	}

	public bool TryCollect(int maxUnloadCubeCount, out CarrierUnloadRequest unloadRequest)
	{
		unloadRequest = null;
		if (maxUnloadCubeCount <= 0)
		{
			return false;
		}
		if (_carrier.RuntimeState.State != 0)
		{
			return false;
		}
		if (MonoSingleton<ConveyorDeliverySystem>.Instance != null && MonoSingleton<ConveyorDeliverySystem>.Instance.IsReceivingCube(_carrier))
		{
			return false;
		}
		Block startBlock = _carrier.BlockController.GetCurrentBlock();
		if (startBlock == null)
		{
			return false;
		}
		EBlockColorType unloadColorType = startBlock.GetBlockColorType();
		_carrier.RuntimeState.SetDeliveryColor(unloadColorType);
		List<Block> unloadingBlocks = new List<Block>();
		List<CarrierCubePayload> payloads = CollectPayloadsFromMatchingBlocks(unloadColorType, maxUnloadCubeCount, unloadingBlocks);
		if (payloads.Count <= 0)
		{
			_carrier.RuntimeState.ClearDeliveryColor();
			return false;
		}
		_carrier.LinkedBlockVisualController?.PrepareForUnload(unloadingBlocks);
		unloadRequest = new CarrierUnloadRequest(_carrier, payloads);
		return true;
	}

	private List<CarrierCubePayload> CollectPayloadsFromMatchingBlocks(EBlockColorType unloadColorType, int maxUnloadCubeCount, List<Block> unloadingBlocks)
	{
		List<CarrierCubePayload> payloads = new List<CarrierCubePayload>();
		Block block;
		while (_carrier.BlockController.TryGetCurrentMatchingBlock(unloadColorType, out block))
		{
			int blockCubeCount = block.GetExpectedUnloadCount();
			if (blockCubeCount <= 0 || payloads.Count + blockCubeCount > maxUnloadCubeCount || !CollectPayloadsFromBlock(block, payloads))
			{
				break;
			}
			unloadingBlocks?.Add(block);
			_carrier.BlockController.CompleteCurrentBlock();
		}
		_carrier.RuntimeState.ClearDeliveryColor();
		return payloads;
	}

	private static bool CollectPayloadsFromBlock(Block block, List<CarrierCubePayload> payloads)
	{
		if (!block.TryBeginUnload())
		{
			return false;
		}
		int startPayloadCount = payloads.Count;
		foreach (BlockCubePayload blockPayload in block.GetUnloadCubePayloadSnapshot())
		{
			payloads.Add(CreateCarrierPayload(block, blockPayload));
		}
		return payloads.Count > startPayloadCount;
	}

	private static CarrierCubePayload CreateCarrierPayload(Block block, BlockCubePayload blockPayload)
	{
		return new CarrierCubePayload(block, blockPayload.WorldPosition, blockPayload.Color, blockPayload.ColorType);
	}
}
