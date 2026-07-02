public sealed class CarrierUnloadPort
{
    private readonly CarrierBase _carrier;
    private readonly CarrierUnloadPayloadCollector _payloadCollector;

    public CarrierUnloadPort(CarrierBase carrier)
    {
        _carrier = carrier;
        _payloadCollector = new CarrierUnloadPayloadCollector(carrier);
    }

    public bool UnloadBlocks()
    {
        if (!_carrier.CanUnloadByMechanic()) return false;
        if (_carrier.BlockController.GetCurrentBlock() == null) return false;

        var capacity = CapacityManager.Instance;
        var maxCubeCount = capacity != null ? capacity.RemainingCubeCapacity : int.MaxValue;
        if (maxCubeCount <= 0) return false;

        CarrierUnloadRequest request;
        if (!_payloadCollector.TryCollect(maxCubeCount, out request)) return false;

        var delivery = ConveyorDeliverySystem.Instance;
        if (delivery == null || !delivery.TrySpawnCarrierUnload(request))
        {
            _carrier.RuntimeState.ClearDeliveryColor();
            return false;
        }

        if (capacity != null)
        {
            capacity.ReservePendingCubes(request.CubeCount);
        }
        _carrier.RuntimeState.MarkUnloading();
        if (GameEventBus.OnCarrierUnload != null)
        {
            GameEventBus.OnCarrierUnload();
        }
        return true;
    }
}
