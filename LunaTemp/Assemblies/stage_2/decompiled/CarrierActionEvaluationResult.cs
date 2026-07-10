public struct CarrierActionEvaluationResult
{
	public readonly bool IsAllowed;

	public readonly CarrierActionBlock? Block;

	public CarrierActionEvaluationResult(bool isAllowed, CarrierActionBlock? block)
	{
		IsAllowed = isAllowed;
		Block = block;
	}

	public static CarrierActionEvaluationResult Allow()
	{
		return new CarrierActionEvaluationResult(true, null);
	}

	public static CarrierActionEvaluationResult Blocked(CarrierActionBlock block)
	{
		return new CarrierActionEvaluationResult(false, block);
	}
}
