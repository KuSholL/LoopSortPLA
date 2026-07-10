public struct CarrierActionBlock
{
	public readonly ECarrierActionType ActionType;

	public readonly int Priority;

	public readonly string Reason;

	public CarrierActionBlock(ECarrierActionType actionType, int priority, string reason)
	{
		ActionType = actionType;
		Priority = priority;
		Reason = reason;
	}
}
