using System.Collections.Generic;

public class ConveyorLoseDetector
{
	private readonly List<DeliveryCubeState> _deliveryStates;

	private readonly IReadOnlyList<CarrierBase> _activeCarriers;

	private readonly Dictionary<CarrierBase, PickupState> _activePickupStates;

	private readonly List<AnimCube> _activeAnimCubes;

	private readonly ForwardBestSlotPickupRule _pickupRule;

	public ConveyorLoseDetector(List<DeliveryCubeState> deliveryStates, IReadOnlyList<CarrierBase> activeCarriers, Dictionary<CarrierBase, PickupState> activePickupStates, List<AnimCube> activeAnimCubes, ForwardBestSlotPickupRule pickupRule)
	{
		_deliveryStates = deliveryStates;
		_activeCarriers = activeCarriers;
		_activePickupStates = activePickupStates;
		_activeAnimCubes = activeAnimCubes;
		_pickupRule = pickupRule;
	}

	public void OnCheckLose()
	{
		if (CheckLose())
		{
			ELoseReason reason = (MonoSingleton<CapacityManager>.Instance.IsFull ? ELoseReason.CapacityFull : ELoseReason.Deadlock);
			GameEventBus.OnLoseTrigger?.Invoke(reason);
		}
	}

	private bool CheckLose()
	{
		if (!MonoSingleton<CapacityManager>.Instance.IsFull)
		{
			return false;
		}
		if (!IsStable())
		{
			return false;
		}
		if (_deliveryStates.Count == 0)
		{
			return false;
		}
		if (_pickupRule == null || _activeCarriers == null || _activeCarriers.Count == 0)
		{
			return true;
		}
		for (int i = 0; i < _deliveryStates.Count; i++)
		{
			DeliveryCubeState state = _deliveryStates[i];
			if (state == null || state.IsPickedUp || state.Cube == null)
			{
				continue;
			}
			for (int j = 0; j < _activeCarriers.Count; j++)
			{
				CarrierBase carrier = _activeCarriers[j];
				if (!(carrier == null) && _pickupRule.CanPickupTarget(state, carrier, _activeCarriers, false))
				{
					return false;
				}
			}
		}
		return true;
	}

	private bool IsStable()
	{
		if (_activeAnimCubes.Count > 0)
		{
			return false;
		}
		if (_activePickupStates.Count > 0)
		{
			return false;
		}
		List<CarrierBase> spawnedCarriers = MonoSingleton<CarrierSystem>.Instance.CarrierSpawner.SpawnedCarriers;
		foreach (CarrierBase carrier in spawnedCarriers)
		{
			if (carrier.IsDelivering)
			{
				return false;
			}
		}
		return true;
	}
}
