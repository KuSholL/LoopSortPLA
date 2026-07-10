public interface ICarrierEventListener : ICarrierMechanicRuntime
{
	void HandleEvent(Carrier carrier, ICarrierMechanicEvent carrierEvent);
}
