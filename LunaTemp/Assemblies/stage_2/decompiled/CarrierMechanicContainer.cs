using System.Collections.Generic;

public sealed class CarrierMechanicContainer
{
	private readonly List<ICarrierMechanicRuntime> _mechanics = new List<ICarrierMechanicRuntime>();

	public IReadOnlyList<ICarrierMechanicRuntime> Mechanics => _mechanics;

	public void Rebuild(IEnumerable<CarrierMechanicData> mechanicDatas)
	{
		_mechanics.Clear();
		if (mechanicDatas == null)
		{
			return;
		}
		foreach (CarrierMechanicData mechanicData in mechanicDatas)
		{
			ICarrierMechanicRuntime mechanic = CreateMechanicRuntime(mechanicData);
			if (mechanic != null)
			{
				_mechanics.Add(mechanic);
			}
		}
	}

	public void RemoveMechanic(ECarrierMechanic type)
	{
		_mechanics.RemoveAll((ICarrierMechanicRuntime m) => m.Type == type);
	}

	public void Reset(Carrier carrier)
	{
		foreach (ICarrierMechanicRuntime mechanic in _mechanics)
		{
			if (mechanic is ICarrierResettableMechanic resettable)
			{
				resettable.Reset(carrier);
			}
		}
	}

	public void DispatchEvent(Carrier carrier, ICarrierMechanicEvent carrierEvent)
	{
		foreach (ICarrierMechanicRuntime mechanic in _mechanics)
		{
			if (mechanic is ICarrierEventListener listener)
			{
				listener.HandleEvent(carrier, carrierEvent);
			}
		}
	}

	private static ICarrierMechanicRuntime CreateMechanicRuntime(CarrierMechanicData mechanicData)
	{
		if (mechanicData == null)
		{
			return null;
		}
		if (mechanicData.Type == ECarrierMechanic.HiddenByColor)
		{
			return new HiddenCarrierByColorMechanicRuntime(mechanicData.UnlockColor);
		}
		if (mechanicData.Type == ECarrierMechanic.SpecialColorReceiver)
		{
			EBlockColorType targetColor = ((mechanicData.TargetColor != EBlockColorType.None) ? mechanicData.TargetColor : mechanicData.UnlockColor);
			return new SpecialColorReceiverMechanicRuntime(targetColor);
		}
		return null;
	}
}
