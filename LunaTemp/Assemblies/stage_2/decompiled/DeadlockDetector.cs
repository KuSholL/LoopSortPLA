using System.Collections.Generic;
using System.Runtime.CompilerServices;

public static class DeadlockDetector
{
	private struct CarrierUnloadSequenceInfo
	{
		public CarrierBase Carrier
		{
			
			get;
		}

		public EBlockColorType Color
		{
			
			get;
		}

		public int UnloadableChainLength
		{
			
			get;
		}

		public CarrierUnloadSequenceInfo(CarrierBase carrier, EBlockColorType color, int unloadableChainLength)
		{
			Carrier = carrier;
			Color = color;
			UnloadableChainLength = unloadableChainLength;
		}
	}

	public static bool IsGameDeadlocked()
	{
		if (!CanCheckDeadlock())
		{
			return false;
		}
		if (MonoSingleton<CapacityManager>.Instance == null)
		{
			return false;
		}
		int remainingCapacity = MonoSingleton<CapacityManager>.Instance.RemainingCubeCapacity;
		int cubePerBlock = MonoSingleton<CapacityManager>.Instance.CubePerBlock;
		if (!TryResolveCapacityRule(remainingCapacity, cubePerBlock, out var requiredRunLength))
		{
			return false;
		}
		if (MonoSingleton<ConveyorDeliverySystem>.Instance.CanAnyConveyorCubeBeReceived())
		{
			return false;
		}
		IReadOnlyList<CarrierBase> spawnedCarriers = GetSpawnedCarriers();
		if (spawnedCarriers == null || spawnedCarriers.Count == 0)
		{
			return false;
		}
		List<CarrierUnloadSequenceInfo> unloadableInfos = GetUnloadableCarrierInfos(spawnedCarriers);
		if (unloadableInfos.Count == 0)
		{
			return false;
		}
		if (!AllUnloadableCarriersMatchPattern(unloadableInfos, requiredRunLength))
		{
			return false;
		}
		for (int i = 0; i < unloadableInfos.Count; i++)
		{
			if (CanReachAnotherCarrier(unloadableInfos[i], spawnedCarriers))
			{
				return false;
			}
		}
		return true;
	}

	private static bool CanCheckDeadlock()
	{
		if (MonoSingleton<LevelManager>.Instance == null || MonoSingleton<LevelManager>.Instance.IsGameEnded || MonoSingleton<LevelManager>.Instance.IsTutorial)
		{
			return false;
		}
		return MonoSingleton<ConveyorDeliverySystem>.Instance != null && MonoSingleton<ConveyorDeliverySystem>.Instance.IsConveyorStable();
	}

	private static bool TryResolveCapacityRule(int remainingCapacity, int cubePerBlock, out int requiredRunLength)
	{
		requiredRunLength = 0;
		if (cubePerBlock <= 0)
		{
			return false;
		}
		if (remainingCapacity == cubePerBlock)
		{
			requiredRunLength = 2;
			return true;
		}
		if (remainingCapacity == cubePerBlock * 2)
		{
			requiredRunLength = 3;
			return true;
		}
		return false;
	}

	private static IReadOnlyList<CarrierBase> GetSpawnedCarriers()
	{
		return (MonoSingleton<CarrierSystem>.Instance != null && MonoSingleton<CarrierSystem>.Instance.CarrierSpawner != null) ? MonoSingleton<CarrierSystem>.Instance.CarrierSpawner.SpawnedCarriers : null;
	}

	private static List<CarrierUnloadSequenceInfo> GetUnloadableCarrierInfos(IReadOnlyList<CarrierBase> spawnedCarriers)
	{
		List<CarrierUnloadSequenceInfo> infos = new List<CarrierUnloadSequenceInfo>();
		if (spawnedCarriers == null)
		{
			return infos;
		}
		for (int i = 0; i < spawnedCarriers.Count; i++)
		{
			CarrierBase carrier = spawnedCarriers[i];
			if (CanParticipateAsUnloadSource(carrier) && TryGetUnloadSequenceInfo(carrier, out var info))
			{
				infos.Add(info);
			}
		}
		return infos;
	}

	private static bool CanParticipateAsUnloadSource(CarrierBase carrier)
	{
		if (carrier == null)
		{
			return false;
		}
		if (carrier.RuntimeState != null && carrier.RuntimeState.IsFinished)
		{
			return false;
		}
		if (carrier.IsLockedByContainer())
		{
			return false;
		}
		if (!carrier.Interactable)
		{
			return false;
		}
		if (!carrier.CanUnloadByMechanic())
		{
			return false;
		}
		if (carrier.BlockController == null || carrier.BlockLayout == null)
		{
			return false;
		}
		return carrier.BlockController.GetTopUnloadCandidateBlock() != null;
	}

	private static bool TryGetUnloadSequenceInfo(CarrierBase carrier, out CarrierUnloadSequenceInfo info)
	{
		info = default(CarrierUnloadSequenceInfo);
		if (carrier == null || carrier.BlockController == null)
		{
			return false;
		}
		Block topBlock = carrier.BlockController.GetTopUnloadCandidateBlock();
		if (topBlock == null)
		{
			return false;
		}
		EBlockColorType color = topBlock.GetBlockColorType();
		List<Block> unloadBlocks = carrier.BlockController.GetPotentialUnloadBlocks(color);
		if (unloadBlocks == null || unloadBlocks.Count == 0)
		{
			return false;
		}
		info = new CarrierUnloadSequenceInfo(carrier, color, unloadBlocks.Count);
		return true;
	}

	private static bool AllUnloadableCarriersMatchPattern(List<CarrierUnloadSequenceInfo> unloadableInfos, int requiredRunLength)
	{
		if (unloadableInfos == null || unloadableInfos.Count == 0)
		{
			return false;
		}
		int patternLength = unloadableInfos[0].UnloadableChainLength;
		if (patternLength < requiredRunLength)
		{
			return false;
		}
		for (int i = 1; i < unloadableInfos.Count; i++)
		{
			if (unloadableInfos[i].UnloadableChainLength != patternLength)
			{
				return false;
			}
		}
		return true;
	}

	private static bool CanReachAnotherCarrier(CarrierUnloadSequenceInfo sourceInfo, IReadOnlyList<CarrierBase> spawnedCarriers)
	{
		if (spawnedCarriers == null)
		{
			return false;
		}
		for (int i = 0; i < spawnedCarriers.Count; i++)
		{
			CarrierBase targetCarrier = spawnedCarriers[i];
			if (CanReceiveFromAnotherCarrier(targetCarrier, sourceInfo.Carrier, sourceInfo.Color))
			{
				return true;
			}
		}
		return false;
	}

	private static bool CanReceiveFromAnotherCarrier(CarrierBase targetCarrier, CarrierBase sourceCarrier, EBlockColorType color)
	{
		if (targetCarrier == null || targetCarrier == sourceCarrier)
		{
			return false;
		}
		if (targetCarrier.RuntimeState != null && targetCarrier.RuntimeState.IsFinished)
		{
			return false;
		}
		if (!targetCarrier.Interactable || targetCarrier.IsDelivering)
		{
			return false;
		}
		return targetCarrier.CanPotentiallyReceive(color);
	}
}
