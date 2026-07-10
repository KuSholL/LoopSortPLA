public interface ICarrierUnloadRuleProvider : ICarrierMechanicRuntime
{
	CarrierActionBlock? GetUnloadBlock(Carrier carrier);
}
