using System.Collections.Generic;

public sealed class ForwardBestSlotPickupRule
{
	private readonly ConveyorPickupHandler _pickupHandler;

	public ForwardBestSlotPickupRule(ConveyorPickupHandler pickupHandler)
	{
		_pickupHandler = pickupHandler;
	}

	public bool CanPickupTarget(DeliveryCubeState state, CarrierBase targetCarrier, IReadOnlyList<CarrierBase> activeCarriers, bool isCanReturnToSourceCarrier = true)
	{
		if (!AutoplayInterface.CanCarrierReceiveColor(targetCarrier, state.BlockColorType))
		{
			return false;
		}
		if (!CanUseCarrier(state, targetCarrier, isCanReturnToSourceCarrier))
		{
			return false;
		}
		if (targetCarrier.IsSpecialReceiverForColor(state.BlockColorType))
		{
			return targetCarrier.CanPotentiallyReceive(state.BlockColorType);
		}
		if (IsReservedForSpecialReceiver(state.BlockColorType, targetCarrier, activeCarriers))
		{
			return false;
		}
		if (IsOccupiedOneWayCarrier(targetCarrier, state.BlockColorType))
		{
			if (CanReceiveOnOccupiedCarrier(state, targetCarrier))
			{
				return !HasOtherHigherPriorityOccupiedCarrier(state, targetCarrier, activeCarriers);
			}
			return false;
		}
		if (HasOtherReceivableOccupiedOneWayCarrier(state, targetCarrier, activeCarriers))
		{
			return false;
		}
		if (IsCarrierEmpty(targetCarrier))
		{
			if (!CanReceiveOnEmptyCarrier(state, targetCarrier))
			{
				return false;
			}
			return !HasOtherReceivableOccupiedCarrier(state, targetCarrier, activeCarriers);
		}
		if (!CanReceiveOnOccupiedCarrier(state, targetCarrier))
		{
			return false;
		}
		return !HasOtherHigherPriorityOccupiedCarrier(state, targetCarrier, activeCarriers);
	}

	private bool HasOtherReceivableOccupiedCarrier(DeliveryCubeState state, CarrierBase targetCarrier, IReadOnlyList<CarrierBase> activeCarriers)
	{
		if (activeCarriers == null || targetCarrier == null)
		{
			return false;
		}
		for (int i = 0; i < activeCarriers.Count; i++)
		{
			CarrierBase carrier = activeCarriers[i];
			if (!(carrier == null) && !(carrier == targetCarrier) && !IsCarrierEmpty(carrier) && CanUseCarrier(state, carrier, false) && CanReceiveOnOccupiedCarrier(state, carrier))
			{
				return true;
			}
		}
		return false;
	}

	private static bool IsOccupiedOneWayCarrier(CarrierBase carrier, EBlockColorType colorType)
	{
		if (carrier == null || carrier.MechanicContainer == null)
		{
			return false;
		}
		bool hasOneWay = false;
		foreach (ICarrierMechanicRuntime mechanic in carrier.MechanicContainer.Mechanics)
		{
			if (mechanic != null && mechanic.Type == ECarrierMechanic.OneWay)
			{
				hasOneWay = true;
				break;
			}
		}
		if (!hasOneWay)
		{
			return false;
		}
		return HasTopBlockColor(carrier, colorType);
	}

	private bool HasOtherReceivableOccupiedOneWayCarrier(DeliveryCubeState state, CarrierBase targetCarrier, IReadOnlyList<CarrierBase> activeCarriers)
	{
		if (activeCarriers == null || targetCarrier == null)
		{
			return false;
		}
		for (int i = 0; i < activeCarriers.Count; i++)
		{
			CarrierBase carrier = activeCarriers[i];
			if (!(carrier == null) && !(carrier == targetCarrier) && IsOccupiedOneWayCarrier(carrier, state.BlockColorType) && CanUseCarrier(state, carrier, false) && CanReceiveOnOccupiedCarrier(state, carrier))
			{
				return true;
			}
		}
		return false;
	}

	private bool HasOtherHigherPriorityOccupiedCarrier(DeliveryCubeState state, CarrierBase targetCarrier, IReadOnlyList<CarrierBase> activeCarriers)
	{
		if (state == null || targetCarrier == null || activeCarriers == null)
		{
			return false;
		}
		bool targetIsSingleColorCarrier = HasOnlyTargetColorBlocks(targetCarrier, state.BlockColorType);
		int targetChainLength = CountTopConsecutiveColorBlocks(targetCarrier, state.BlockColorType);
		if (targetChainLength <= 0)
		{
			return false;
		}
		for (int i = 0; i < activeCarriers.Count; i++)
		{
			CarrierBase carrier = activeCarriers[i];
			if (!(carrier == null) && !(carrier == targetCarrier) && !IsCarrierEmpty(carrier) && CanUseCarrier(state, carrier, false) && CanReceiveOnOccupiedCarrier(state, carrier) && HasHigherOccupiedCarrierPriority(carrier, targetCarrier, state.BlockColorType, targetIsSingleColorCarrier, targetChainLength))
			{
				return true;
			}
		}
		return false;
	}

	private static bool HasHigherOccupiedCarrierPriority(CarrierBase candidateCarrier, CarrierBase targetCarrier, EBlockColorType colorType, bool targetIsSingleColorCarrier, int targetChainLength)
	{
		bool candidateIsOneWay = IsOccupiedOneWayCarrier(candidateCarrier, colorType);
		bool targetIsOneWay = IsOccupiedOneWayCarrier(targetCarrier, colorType);
		if (candidateIsOneWay != targetIsOneWay)
		{
			return candidateIsOneWay;
		}
		bool candidateIsSingleColorCarrier = HasOnlyTargetColorBlocks(candidateCarrier, colorType);
		if (candidateIsSingleColorCarrier != targetIsSingleColorCarrier)
		{
			return candidateIsSingleColorCarrier;
		}
		int candidateChainLength = CountTopConsecutiveColorBlocks(candidateCarrier, colorType);
		return candidateChainLength > targetChainLength;
	}

	private bool CanReceiveOnOccupiedCarrier(DeliveryCubeState state, CarrierBase carrier)
	{
		if (carrier == null)
		{
			return false;
		}
		if (!HasTopBlockColor(carrier, state.BlockColorType))
		{
			return false;
		}
		if (!carrier.CanPotentiallyReceive(state.BlockColorType))
		{
			return false;
		}
		return CountReceivableSlotsForColor(carrier, state.BlockColorType) > 0;
	}

	private static bool CanReceiveOnEmptyCarrier(DeliveryCubeState state, CarrierBase carrier)
	{
		return carrier != null && carrier.CanPotentiallyReceive(state.BlockColorType);
	}

	private bool CanReceiveOnCarrier(DeliveryCubeState state, CarrierBase carrier)
	{
		if (carrier == null)
		{
			return false;
		}
		return IsCarrierEmpty(carrier) ? CanReceiveOnEmptyCarrier(state, carrier) : CanReceiveOnOccupiedCarrier(state, carrier);
	}

	private static bool IsReservedForSpecialReceiver(EBlockColorType colorType, CarrierBase targetCarrier, IReadOnlyList<CarrierBase> activeCarriers)
	{
		if (targetCarrier != null && targetCarrier.IsSpecialReceiverForColor(colorType))
		{
			return false;
		}
		return HasSpecialReceiverForColor(colorType, activeCarriers);
	}

	private static bool HasSpecialReceiverForColor(EBlockColorType colorType, IReadOnlyList<CarrierBase> activeCarriers)
	{
		if (activeCarriers == null)
		{
			return false;
		}
		for (int i = 0; i < activeCarriers.Count; i++)
		{
			CarrierBase carrier = activeCarriers[i];
			if (carrier != null && carrier.IsSpecialReceiverForColor(colorType))
			{
				return true;
			}
		}
		return false;
	}

	private bool CanUseCarrier(DeliveryCubeState state, CarrierBase carrier, bool isCanReturnToSourceCarrier = true)
	{
		if (state == null || state.Cube == null || state.IsPickedUp)
		{
			return false;
		}
		if (carrier == null || !carrier.Interactable || carrier.IsDelivering)
		{
			return false;
		}
		if (!state.Cube.HasStartedFirstLoop())
		{
			return false;
		}
		if (isCanReturnToSourceCarrier && !CanReturnToSourceCarrier(state, carrier))
		{
			return false;
		}
		return _pickupHandler != null && _pickupHandler.CanPickupColor(carrier, state.BlockColorType);
	}

	private static bool CanReturnToSourceCarrier(DeliveryCubeState state, CarrierBase targetCarrier)
	{
		if (state.SourceCarrier != targetCarrier)
		{
			return true;
		}
		return state.Cube != null && state.Cube.HasCompletedLap();
	}

	private static bool IsCarrierEmpty(CarrierBase carrier)
	{
		if (carrier == null || carrier.BlockLayout == null)
		{
			return true;
		}
		for (int i = 0; i < carrier.MaxBlockCount; i++)
		{
			Block block = carrier.BlockLayout.GetBlockByIndex(i);
			if (block != null && block.HasContent)
			{
				return false;
			}
		}
		return true;
	}

	private static bool HasTopBlockColor(CarrierBase carrier, EBlockColorType colorType)
	{
		Block block;
		return TryGetTopContentBlock(carrier, out block) && block.GetBlockColorType() == colorType;
	}

	private static int CountTopConsecutiveColorBlocks(CarrierBase carrier, EBlockColorType colorType)
	{
		if (carrier == null || carrier.BlockLayout == null)
		{
			return 0;
		}
		int count = 0;
		bool foundTopContent = false;
		for (int i = 0; i < carrier.MaxBlockCount; i++)
		{
			Block block = carrier.BlockLayout.GetBlockByIndex(i);
			if (block == null || !block.HasContent)
			{
				if (foundTopContent)
				{
					break;
				}
				continue;
			}
			foundTopContent = true;
			if (block.GetBlockColorType() != colorType)
			{
				break;
			}
			count++;
		}
		return count;
	}

	private static bool HasOnlyTargetColorBlocks(CarrierBase carrier, EBlockColorType colorType)
	{
		if (carrier == null || carrier.BlockLayout == null)
		{
			return false;
		}
		bool hasContent = false;
		for (int i = 0; i < carrier.MaxBlockCount; i++)
		{
			Block block = carrier.BlockLayout.GetBlockByIndex(i);
			if (!(block == null) && block.HasContent)
			{
				hasContent = true;
				if (block.GetBlockColorType() != colorType)
				{
					return false;
				}
			}
		}
		return hasContent;
	}

	private static bool TryGetTopContentBlock(CarrierBase carrier, out Block topBlock)
	{
		topBlock = null;
		if (carrier == null || carrier.BlockLayout == null)
		{
			return false;
		}
		for (int i = 0; i < carrier.MaxBlockCount; i++)
		{
			Block block = carrier.BlockLayout.GetBlockByIndex(i);
			if (!(block == null) && block.HasContent)
			{
				topBlock = block;
				return true;
			}
		}
		return false;
	}

	private static int CountReceivableSlotsForColor(CarrierBase carrier, EBlockColorType colorType)
	{
		if (carrier == null || carrier.BlockLayout == null)
		{
			return 0;
		}
		if (!carrier.CanReceiveByMechanic(colorType))
		{
			return 0;
		}
		int slotCount = 0;
		bool hasMatchingContent = false;
		for (int i = 0; i < carrier.MaxBlockCount; i++)
		{
			Block block = carrier.BlockLayout.GetBlockByIndex(i);
			if (block == null)
			{
				continue;
			}
			if (!block.HasContent)
			{
				int openSlots = GetOpenSlotCount(block);
				if (openSlots > 0)
				{
					if (!block.IsAvailableForReceive(colorType))
					{
						break;
					}
					slotCount += openSlots;
				}
				continue;
			}
			if (block.GetBlockColorType() != colorType)
			{
				break;
			}
			hasMatchingContent = true;
			int sameColorOpenSlots = GetOpenSlotCount(block);
			if (sameColorOpenSlots > 0)
			{
				if (!block.IsAvailableForReceive(colorType))
				{
					break;
				}
				slotCount += sameColorOpenSlots;
			}
		}
		return hasMatchingContent ? slotCount : 0;
	}

	private static int GetOpenSlotCount(Block block)
	{
		if (block == null)
		{
			return 0;
		}
		int slotCount = block.GetMaxCubes() - block.GetCurrentCubes();
		return (slotCount > 0) ? slotCount : 0;
	}
}
