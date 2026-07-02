public interface IConveyorPickupHandler
{
    void TryPickupCube(Cube cube, CarrierBase targetCarrier);
    void ClearPickupStates();
}
