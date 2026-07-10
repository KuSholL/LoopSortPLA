public sealed class HiddenCarrierByColorMechanicRuntime : ICarrierInteractRuleProvider, ICarrierMechanicRuntime, ICarrierUnloadRuleProvider, ICarrierReceiveRuleProvider, ICarrierVisualRequestProvider, ICarrierEventListener, ICarrierResettableMechanic
{
	private const int LockPriority = 100;

	private readonly EBlockColorType _unlockColor;

	private bool _isUnlocked;

	public ECarrierMechanic Type => ECarrierMechanic.HiddenByColor;

	public bool IsUnlocked => _isUnlocked;

	public HiddenCarrierByColorMechanicRuntime(EBlockColorType unlockColor)
	{
		_unlockColor = unlockColor;
	}

	public CarrierActionBlock? GetInteractBlock(Carrier carrier)
	{
		return _isUnlocked ? null : new CarrierActionBlock?(new CarrierActionBlock(ECarrierActionType.Interact, 100, "HiddenByColor"));
	}

	public CarrierActionBlock? GetUnloadBlock(Carrier carrier)
	{
		return _isUnlocked ? null : new CarrierActionBlock?(new CarrierActionBlock(ECarrierActionType.Unload, 100, "HiddenByColor"));
	}

	public CarrierActionBlock? GetReceiveBlock(Carrier carrier, EBlockColorType colorType)
	{
		return _isUnlocked ? null : new CarrierActionBlock?(new CarrierActionBlock(ECarrierActionType.Receive, 100, "HiddenByColor"));
	}

	public CarrierVisualRequest GetVisualRequest(Carrier carrier)
	{
		if (_isUnlocked)
		{
			return null;
		}
		return new CarrierVisualRequest
		{
			Kind = ECarrierVisualKind.HiddenShell,
			Priority = 100,
			ColorType = _unlockColor,
			HideCarrierRenderers = false
		};
	}

	public void HandleEvent(Carrier carrier, ICarrierMechanicEvent carrierEvent)
	{
		if (!_isUnlocked && carrierEvent is CarrierFinishedColorEvent && ((CarrierFinishedColorEvent)(object)carrierEvent).ColorType == _unlockColor)
		{
			_isUnlocked = true;
			carrier.RefreshMechanicVisualState();
		}
	}

	public void Reset(Carrier carrier)
	{
		_isUnlocked = false;
	}
}
