using UnityEngine;

/// <summary>
/// Lưu trạng thái của một cube đang chạy trên conveyor sau khi được unload từ carrier.
/// </summary>
public sealed class DeliveryCubeState
{
    public readonly Cube Cube;
    public readonly CarrierBase SourceCarrier;
    public readonly EBlockColorType BlockColorType;
    public readonly Color Color;
    public readonly int UndoBatchId;
    public bool IsPickedUp;

    public float PreviousProgress;
    public float PreviousProgressCorner;
    public float SpawnProgress { get; private set; }
    public bool CanReturnToSourceCarrier { get; private set; }
    private bool _hasLeftSourcePickupZone;

    /// <summary>
    /// Khởi tạo state cho cube đang chạy trên conveyor.
    /// </summary>
    public DeliveryCubeState(Cube cube, CarrierBase sourceCarrier, EBlockColorType blockColorType, Color color, int undoBatchId)
    {
        Cube = cube;
        SourceCarrier = sourceCarrier;
        BlockColorType = blockColorType;
        Color = color;
        UndoBatchId = undoBatchId;
        SpawnProgress = cube != null ? Mathf.Repeat(cube.GetProgress(), 1f) : 0f;
        CanReturnToSourceCarrier = sourceCarrier == null;
    }

    public void TrackProgress(float currentProgress)
    {
        if (CanReturnToSourceCarrier || _hasLeftSourcePickupZone || SourceCarrier == null) return;

        var forwardDistanceFromSpawn = Mathf.Repeat(
            Mathf.Repeat(currentProgress, 1f) - SpawnProgress,
            1f);

        // Ignore the two seam zones. A small physics push backwards at spawn appears as a value
        // close to one after Repeat(), while a real forward trip must pass through this middle band.
        if (forwardDistanceFromSpawn >= 0.15f && forwardDistanceFromSpawn <= 0.85f)
        {
            _hasLeftSourcePickupZone = true;
        }
    }

    public bool IsInsidePickupRetryWindow(
        CarrierBase carrier,
        float currentProgress,
        float pickupProgress,
        float retryWindow)
    {
        if (carrier == null || retryWindow <= 0f) return false;

        // Do not let a cube be taken back by its source while it is still spawning at
        // that exact pickup point. Once it has travelled away, the same window is safe.
        if (carrier == SourceCarrier && !CanReturnToSourceCarrier && !_hasLeftSourcePickupZone)
        {
            return false;
        }

        var distanceAfterPickup = Mathf.Repeat(
            Mathf.Repeat(currentProgress, 1f) - Mathf.Repeat(pickupProgress, 1f),
            1f);
        return distanceAfterPickup <= retryWindow;
    }

    /// <summary>
    /// Arms a return to the source only after this cube really crosses the source pickup point.
    /// The normal carrier prefab puts its pickup point behind the unload point, so this crossing
    /// represents a complete conveyor loop. Using the actual crossing also remains stable when
    /// physics contact makes the sampled progress move backwards briefly.
    /// </summary>
    public void NotifyPickupPointReached(CarrierBase carrier, float pickupProgress)
    {
        if (CanReturnToSourceCarrier || carrier == null || carrier != SourceCarrier) return;

        // Once a cube has left the source zone, the next crossing of this same pickup point is its
        // return pass. This is more reliable than deriving a lap only from accumulated rigidbody
        // motion, which can temporarily go backwards while crowded cubes collide.
        if (_hasLeftSourcePickupZone || (Cube != null && Cube.HasCompletedLap()))
        {
            CanReturnToSourceCarrier = true;
        }
    }
}
