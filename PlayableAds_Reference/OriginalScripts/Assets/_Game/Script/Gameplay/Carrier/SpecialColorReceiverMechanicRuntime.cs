public sealed class SpecialColorReceiverMechanicRuntime :
    ICarrierReceiveRuleProvider,
    ISpecialColorReceiverMechanic,
    ICarrierVisualRequestProvider
{
    private const int LockPriority = 95;

    public SpecialColorReceiverMechanicRuntime(EBlockColorType targetColor)
    {
        TargetColor = targetColor;
    }

    public ECarrierMechanic Type => ECarrierMechanic.SpecialColorReceiver;
    public EBlockColorType TargetColor { get; }

    public CarrierActionBlock? GetReceiveBlock(Carrier carrier, EBlockColorType colorType)
    {
        if (colorType == TargetColor) return null;

        return new CarrierActionBlock(
            ECarrierActionType.Receive,
            LockPriority,
            "SpecialColorReceiver");
    }

    public CarrierVisualRequest GetVisualRequest(Carrier carrier)
    {
        return new CarrierVisualRequest
        {
            Kind = ECarrierVisualKind.SpecialColorReceiver,
            Priority = LockPriority,
            ColorType = TargetColor,
            HideCarrierRenderers = true
        };
    }
}
