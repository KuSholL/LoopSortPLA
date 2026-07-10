using System;

[Serializable]
public sealed class CarrierMechanicData
{
	public ECarrierMechanic Type;

	public EBlockColorType UnlockColor = EBlockColorType.Red;

	public EBlockColorType TargetColor = EBlockColorType.None;
}
