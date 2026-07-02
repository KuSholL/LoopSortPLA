using System;

public static class CarrierMechanicEventHub
{
    public static event Action<ICarrierMechanicEvent> OnEvent;

    public static void Publish(ICarrierMechanicEvent carrierEvent)
    {
        OnEvent?.Invoke(carrierEvent);
    }
}
