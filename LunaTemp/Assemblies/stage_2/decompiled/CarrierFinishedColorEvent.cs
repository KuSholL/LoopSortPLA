public struct CarrierFinishedColorEvent : ICarrierMechanicEvent
{
	public readonly EBlockColorType ColorType;

	public CarrierFinishedColorEvent(EBlockColorType colorType)
	{
		ColorType = colorType;
	}
}
