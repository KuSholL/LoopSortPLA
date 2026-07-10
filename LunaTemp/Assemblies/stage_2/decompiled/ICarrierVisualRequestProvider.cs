public interface ICarrierVisualRequestProvider : ICarrierMechanicRuntime
{
	CarrierVisualRequest GetVisualRequest(Carrier carrier);
}
