using System.Collections.Generic;

public sealed class CarrierUnloadRequest
{
	private static int _nextUndoBatchId;

	public int UndoBatchId { get; }

	public CarrierBase SourceCarrier { get; }

	public int CarrierSessionId { get; }

	public IReadOnlyList<CarrierCubePayload> CubePayloads { get; }

	public int CubeCount => CubePayloads?.Count ?? 0;

	public CarrierUnloadRequest(CarrierBase sourceCarrier, IReadOnlyList<CarrierCubePayload> cubePayloads)
	{
		UndoBatchId = CreateUndoBatchId();
		SourceCarrier = sourceCarrier;
		CarrierSessionId = ((sourceCarrier != null) ? sourceCarrier.SessionId : 0);
		CubePayloads = cubePayloads;
	}

	private static int CreateUndoBatchId()
	{
		_nextUndoBatchId++;
		if (_nextUndoBatchId <= 0)
		{
			_nextUndoBatchId = 1;
		}
		return _nextUndoBatchId;
	}
}
