public interface ICarrierReceiveRuleProvider : ICarrierMechanicRuntime
{
	CarrierActionBlock? GetReceiveBlock(Carrier carrier, EBlockColorType colorType);
}
