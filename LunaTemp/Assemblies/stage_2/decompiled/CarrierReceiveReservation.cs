using UnityEngine;

public struct CarrierReceiveReservation
{
	public readonly Block TargetBlock;

	public readonly Vector3 TargetPosition;

	public readonly EBlockColorType BlockColorType;

	public readonly int UndoBatchId;

	public CarrierReceiveReservation(Block targetBlock, Vector3 targetPosition, EBlockColorType blockColorType, int undoBatchId = 0)
	{
		TargetBlock = targetBlock;
		TargetPosition = targetPosition;
		BlockColorType = blockColorType;
		UndoBatchId = undoBatchId;
	}
}
