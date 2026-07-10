public sealed class CarrierVisualResolver
{
	public CarrierVisualRequest Resolve(Carrier carrier)
	{
		CarrierVisualRequest selectedRequest = null;
		foreach (ICarrierMechanicRuntime mechanic in carrier.MechanicContainer.Mechanics)
		{
			if (mechanic is ICarrierVisualRequestProvider requestProvider)
			{
				CarrierVisualRequest nextRequest = requestProvider.GetVisualRequest(carrier);
				if (nextRequest != null && nextRequest.Kind != 0 && (selectedRequest == null || nextRequest.Priority > selectedRequest.Priority))
				{
					selectedRequest = nextRequest;
				}
			}
		}
		return selectedRequest ?? new CarrierVisualRequest();
	}
}
