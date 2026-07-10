public sealed class SpecialColorReceiverMechanicRuntime : ICarrierReceiveRuleProvider, ICarrierMechanicRuntime, ISpecialColorReceiverMechanic, ICarrierVisualRequestProvider
{
	private const int LockPriority = 95;

	public ECarrierMechanic Type => ECarrierMechanic.SpecialColorReceiver;

	public EBlockColorType TargetColor { get; }

	public SpecialColorReceiverMechanicRuntime(EBlockColorType targetColor)
	{
		TargetColor = targetColor;
	}

	public CarrierActionBlock? GetReceiveBlock(Carrier carrier, EBlockColorType colorType)
	{
		if (colorType == TargetColor)
		{
			return null;
		}
		return new CarrierActionBlock(ECarrierActionType.Receive, 95, "SpecialColorReceiver");
	}

	public CarrierVisualRequest GetVisualRequest(Carrier carrier)
	{
		return new CarrierVisualRequest
		{
			Kind = ECarrierVisualKind.SpecialColorReceiver,
			Priority = 95,
			ColorType = TargetColor,
			HideCarrierRenderers = true
		};
	}
}
