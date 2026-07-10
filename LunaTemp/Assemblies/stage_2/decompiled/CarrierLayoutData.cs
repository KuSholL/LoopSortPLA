using System;
using System.Collections.Generic;

[Serializable]
public sealed class CarrierLayoutData
{
	public List<CarrierStackData> Carriers = new List<CarrierStackData>();

	public List<CarrierStackData> BoosterCarriers = new List<CarrierStackData>();

	public List<ContainerLevelData> Containers = new List<ContainerLevelData>();
}
