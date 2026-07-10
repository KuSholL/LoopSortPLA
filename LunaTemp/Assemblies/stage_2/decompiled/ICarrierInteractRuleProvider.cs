public interface ICarrierInteractRuleProvider : ICarrierMechanicRuntime
{
	CarrierActionBlock? GetInteractBlock(Carrier carrier);
}
