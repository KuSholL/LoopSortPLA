using System.Collections.Generic;

/// <summary>
/// Kiểm tra trạng thái deadlock khi conveyor đầy và không còn carrier nào có thể nhận cube.
/// </summary>
public class ConveyorLoseDetector
{   
    private readonly List<DeliveryCubeState> _deliveryStates;
    private readonly IReadOnlyList<CarrierBase> _activeCarriers;
    private readonly Dictionary<CarrierBase, PickupState> _activePickupStates;
    private readonly List<AnimCube> _activeAnimCubes;
    private readonly ForwardBestSlotPickupRule _pickupRule;
    
    public ConveyorLoseDetector(
        List<DeliveryCubeState> deliveryStates,
        IReadOnlyList<CarrierBase> activeCarriers,
        Dictionary<CarrierBase, PickupState> activePickupStates,
        List<AnimCube> activeAnimCubes,
        ForwardBestSlotPickupRule pickupRule)
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
            ELoseReason reason = CapacityManager.Instance.IsFull ? ELoseReason.CapacityFull : ELoseReason.Deadlock;
            GameEventBus.OnLoseTrigger?.Invoke(reason);
        }
    }
    
    private bool CheckLose()
    {
        if (!CapacityManager.Instance.IsFull) return false;
        if (!IsStable()) return false;
        if (_deliveryStates.Count == 0) return false;
        if (_pickupRule == null || _activeCarriers == null || _activeCarriers.Count == 0) return true;

        for (var i = 0; i < _deliveryStates.Count; i++)
        {
            var state = _deliveryStates[i];
            if (state == null || state.IsPickedUp || state.Cube == null) continue;

            for (var j = 0; j < _activeCarriers.Count; j++)
            {
                var carrier = _activeCarriers[j];
                if (carrier == null) continue;

                if (_pickupRule.CanPickupTarget(state, carrier, _activeCarriers, false))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private bool IsStable()
    {
        if (_activeAnimCubes.Count > 0) return false;
        if (_activePickupStates.Count > 0) return false;

        var spawnedCarriers = CarrierSystem.Instance.CarrierSpawner.SpawnedCarriers;
        foreach (var carrier in spawnedCarriers)
        {
            if (carrier.IsDelivering) return false;
        }

        return true;
    }
}
