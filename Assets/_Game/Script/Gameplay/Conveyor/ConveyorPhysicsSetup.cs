using UnityEngine;

public static class ConveyorPhysicsSetup
{
    private const int CarrierLayer = 6;
    private const int DeliveryCubeLayer = 7;
    private const int BlockLayer1 = 8;
    private const int BlockLayer2 = 9;
    private const int BlockLayer3 = 10;
    private const int BlockLayer4 = 11;
    private const int MechanicLayer = 17;

    public static void ConfigureGameplayCollisions()
    {
        SetDeliveryCubeCollisionWith(DeliveryCubeLayer, true);

        // Delivery cubes are moved/received by conveyor progress logic.
        // They still collide with the road/rails on the Default layer and with each other,
        // but static carrier/block/mechanic colliders are not part of the conveyor physics path.
        SetDeliveryCubeCollisionWith(CarrierLayer, false);
        SetDeliveryCubeCollisionWith(BlockLayer1, false);
        SetDeliveryCubeCollisionWith(BlockLayer2, false);
        SetDeliveryCubeCollisionWith(BlockLayer3, false);
        SetDeliveryCubeCollisionWith(BlockLayer4, false);
        SetDeliveryCubeCollisionWith(MechanicLayer, false);
    }

    private static void SetDeliveryCubeCollisionWith(int layer, bool canCollide)
    {
        if (layer < 0 || layer > 31) return;
        Physics.IgnoreLayerCollision(DeliveryCubeLayer, layer, !canCollide);
    }
}
