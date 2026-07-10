public sealed class PickupState
{
	public readonly EBlockColorType BlockColorType;

	public int InFlightCount = 1;

	public PickupState(EBlockColorType blockColorType)
	{
		BlockColorType = blockColorType;
	}
}
