public sealed class HiddenCarrierByColorMechanicRuntime :
    ICarrierInteractRuleProvider,
    ICarrierUnloadRuleProvider,
    ICarrierReceiveRuleProvider,
    ICarrierVisualRequestProvider,
    ICarrierEventListener,
    ICarrierResettableMechanic
{
    private const int LockPriority = 100;

    private readonly EBlockColorType _unlockColor;
    private bool _isUnlocked;

    public HiddenCarrierByColorMechanicRuntime(EBlockColorType unlockColor)
    {
        _unlockColor = unlockColor;
    }

    public ECarrierMechanic Type => ECarrierMechanic.HiddenByColor;

    public CarrierActionBlock? GetInteractBlock(Carrier carrier)
    {
        return _isUnlocked ? null : new CarrierActionBlock(ECarrierActionType.Interact, LockPriority, "HiddenByColor");
    }

    public CarrierActionBlock? GetUnloadBlock(Carrier carrier)
    {
        return _isUnlocked ? null : new CarrierActionBlock(ECarrierActionType.Unload, LockPriority, "HiddenByColor");
    }

    public CarrierActionBlock? GetReceiveBlock(Carrier carrier, EBlockColorType colorType)
    {
        return _isUnlocked ? null : new CarrierActionBlock(ECarrierActionType.Receive, LockPriority, "HiddenByColor");
    }

    public CarrierVisualRequest GetVisualRequest(Carrier carrier)
    {
        return _isUnlocked
            ? null
            : new CarrierVisualRequest
            {
                Kind = ECarrierVisualKind.HiddenShell,
                Priority = LockPriority,
                ColorType = _unlockColor,
                HideCarrierRenderers = false
            };
    }

    public void HandleEvent(Carrier carrier, ICarrierMechanicEvent carrierEvent)
    {
        if (carrierEvent is not CarrierFinishedColorEvent finishedColorEvent) return;
        if (finishedColorEvent.ColorType != _unlockColor) return;
        _isUnlocked = true;
        carrier.RefreshMechanicVisualState();
    }

    public void Reset(Carrier carrier)
    {
        _isUnlocked = false;
        carrier.RefreshMechanicVisualState();
    }
}
