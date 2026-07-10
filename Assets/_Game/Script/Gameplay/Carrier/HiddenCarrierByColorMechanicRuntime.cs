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
    public bool IsUnlocked => _isUnlocked;

    public CarrierActionBlock? GetInteractBlock(Carrier carrier)
    {
        return _isUnlocked
            ? (CarrierActionBlock?)null
            : new CarrierActionBlock(ECarrierActionType.Interact, LockPriority, "HiddenByColor");
    }

    public CarrierActionBlock? GetUnloadBlock(Carrier carrier)
    {
        return _isUnlocked
            ? (CarrierActionBlock?)null
            : new CarrierActionBlock(ECarrierActionType.Unload, LockPriority, "HiddenByColor");
    }

    public CarrierActionBlock? GetReceiveBlock(Carrier carrier, EBlockColorType colorType)
    {
        return _isUnlocked
            ? (CarrierActionBlock?)null
            : new CarrierActionBlock(ECarrierActionType.Receive, LockPriority, "HiddenByColor");
    }

    public CarrierVisualRequest GetVisualRequest(Carrier carrier)
    {
        if (_isUnlocked) return null;
        return new CarrierVisualRequest
        {
            Kind = ECarrierVisualKind.HiddenShell,
            Priority = LockPriority,
            ColorType = _unlockColor,
            HideCarrierRenderers = false
        };
    }

    public void HandleEvent(Carrier carrier, ICarrierMechanicEvent carrierEvent)
    {
        if (_isUnlocked || !(carrierEvent is CarrierFinishedColorEvent)) return;
        var finishedEvent = (CarrierFinishedColorEvent)carrierEvent;
        if (finishedEvent.ColorType != _unlockColor) return;
        _isUnlocked = true;
        carrier.RefreshMechanicVisualState();
    }

    public void Reset(Carrier carrier)
    {
        _isUnlocked = false;
    }
}
