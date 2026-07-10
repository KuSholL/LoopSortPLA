public sealed class CarrierActionGateResolver
{
	public CarrierActionEvaluationResult EvaluateInteract(Carrier carrier)
	{
		return Evaluate(carrier, ECarrierActionType.Interact, null);
	}

	public CarrierActionEvaluationResult EvaluateUnload(Carrier carrier)
	{
		return Evaluate(carrier, ECarrierActionType.Unload, null);
	}

	public CarrierActionEvaluationResult EvaluateReceive(Carrier carrier, EBlockColorType colorType)
	{
		return Evaluate(carrier, ECarrierActionType.Receive, colorType);
	}

	private CarrierActionEvaluationResult Evaluate(Carrier carrier, ECarrierActionType actionType, EBlockColorType? colorType)
	{
		if (carrier.IsLockedByContainer())
		{
			return CarrierActionEvaluationResult.Blocked(new CarrierActionBlock(actionType, 100, "Locked by container"));
		}
		CarrierActionBlock? highestPriorityBlock = null;
		foreach (ICarrierMechanicRuntime mechanic in carrier.MechanicContainer.Mechanics)
		{
			CarrierActionBlock? nextBlock = null;
			switch (actionType)
			{
			case ECarrierActionType.Interact:
				if (mechanic is ICarrierInteractRuleProvider interactProvider)
				{
					nextBlock = interactProvider.GetInteractBlock(carrier);
				}
				break;
			case ECarrierActionType.Unload:
				if (mechanic is ICarrierUnloadRuleProvider unloadProvider)
				{
					nextBlock = unloadProvider.GetUnloadBlock(carrier);
				}
				break;
			case ECarrierActionType.Receive:
				if (colorType.HasValue && mechanic is ICarrierReceiveRuleProvider receiveProvider)
				{
					nextBlock = receiveProvider.GetReceiveBlock(carrier, colorType.Value);
				}
				break;
			}
			if (nextBlock.HasValue && (!highestPriorityBlock.HasValue || nextBlock.Value.Priority > highestPriorityBlock.Value.Priority))
			{
				highestPriorityBlock = nextBlock;
			}
		}
		return highestPriorityBlock.HasValue ? CarrierActionEvaluationResult.Blocked(highestPriorityBlock.Value) : CarrierActionEvaluationResult.Allow();
	}
}
