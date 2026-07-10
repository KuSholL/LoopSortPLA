public sealed class CarrierRuntimeState
{
	public CarrierStateType State { get; private set; } = CarrierStateType.Idle;


	public int CurrentBlockIndex { get; private set; }

	public bool HasDeliveryColor { get; private set; }

	public EBlockColorType DeliveryColor { get; private set; }

	public bool HasCompletedAllBlocks { get; private set; }

	public bool IsIdle => State == CarrierStateType.Idle;

	public bool IsCompleted => State == CarrierStateType.Completed;

	public bool IsUnloading => State == CarrierStateType.Unloading;

	public bool IsFinished => State == CarrierStateType.Finished;

	public void Reset()
	{
		State = CarrierStateType.Idle;
		CurrentBlockIndex = 0;
		HasDeliveryColor = false;
		DeliveryColor = EBlockColorType.Red;
		HasCompletedAllBlocks = false;
	}

	public void SetDeliveryColor(EBlockColorType colorType)
	{
		DeliveryColor = colorType;
		HasDeliveryColor = true;
	}

	public void ClearDeliveryColor()
	{
		HasDeliveryColor = false;
		DeliveryColor = EBlockColorType.Red;
	}

	public void MarkUnloading()
	{
		State = CarrierStateType.Unloading;
	}

	public void FinishUnloading()
	{
		ClearDeliveryColor();
		if (State == CarrierStateType.Unloading)
		{
			State = (HasCompletedAllBlocks ? CarrierStateType.Completed : CarrierStateType.Idle);
		}
	}

	public void MarkCompleted()
	{
		HasCompletedAllBlocks = true;
		if (State != CarrierStateType.Unloading)
		{
			State = CarrierStateType.Completed;
		}
	}

	public void MarkFinished()
	{
		State = CarrierStateType.Finished;
	}

	public void ClearFinished()
	{
		if (State == CarrierStateType.Finished)
		{
			State = CarrierStateType.Idle;
		}
	}

	public void SetCurrentBlockIndex(int blockIndex)
	{
		CurrentBlockIndex = blockIndex;
	}

	public void WakeAtBlock(int blockIndex)
	{
		HasCompletedAllBlocks = false;
		if (State == CarrierStateType.Completed)
		{
			State = CarrierStateType.Idle;
		}
		if (CurrentBlockIndex > blockIndex)
		{
			CurrentBlockIndex = blockIndex;
		}
	}
}
