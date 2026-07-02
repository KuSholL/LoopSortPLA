public sealed class OneWayCarrierMechanicRuntime :
    ICarrierInteractRuleProvider,
    ICarrierUnloadRuleProvider,
    ICarrierVisualRequestProvider
{
    private const int LockPriority = 20;

    public ECarrierMechanic Type => ECarrierMechanic.OneWay;

    public CarrierActionBlock? GetInteractBlock(Carrier carrier)
    {
        return new CarrierActionBlock(ECarrierActionType.Interact, LockPriority, "OneWay");
    }

    public CarrierActionBlock? GetUnloadBlock(Carrier carrier)
    {
        return new CarrierActionBlock(ECarrierActionType.Unload, LockPriority, "OneWay");
    }

    public CarrierVisualRequest GetVisualRequest(Carrier carrier)
    {
        return new CarrierVisualRequest
        {
            Kind = ECarrierVisualKind.OneWayOverlay,
            Priority = LockPriority,
            HideCarrierRenderers = false
        };
    }
}
