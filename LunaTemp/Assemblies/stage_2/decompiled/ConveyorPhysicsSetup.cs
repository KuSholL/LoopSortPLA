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
		SetDeliveryCubeCollisionWith(7, true);
		SetDeliveryCubeCollisionWith(6, false);
		SetDeliveryCubeCollisionWith(8, false);
		SetDeliveryCubeCollisionWith(9, false);
		SetDeliveryCubeCollisionWith(10, false);
		SetDeliveryCubeCollisionWith(11, false);
		SetDeliveryCubeCollisionWith(17, false);
	}

	private static void SetDeliveryCubeCollisionWith(int layer, bool canCollide)
	{
		if (layer >= 0 && layer <= 31)
		{
			Physics.IgnoreLayerCollision(7, layer, !canCollide);
		}
	}
}
