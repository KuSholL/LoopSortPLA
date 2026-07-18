using System;
using UnityEngine;

public class Cube : CubeBase
{
    [SerializeField] private CubeMovement cubeMovement;

    public void Setup(ConveyorPathRuntime splineContainer, float startProgress = 0f, float progressOffset = 0f, Vector3? initialWorldPosition = null)
    {
        cubeMovement.Setup(splineContainer, startProgress, progressOffset, initialWorldPosition);
    }

    public bool HasCompletedLap()
    {
        return cubeMovement && cubeMovement.HasCompletedLap();
    }
    
    public bool HasStartedFirstLoop()
    {
        return cubeMovement && cubeMovement.HasStartedFirstLoop();
    }

    public float GetProgress()
    {
        return cubeMovement ? cubeMovement.GetProgress() : 0f;
    }

    public float GetRealtimeProgress()
    {
        return cubeMovement ? cubeMovement.GetRealtimeProgress() : 0f;
    }

    public void SyncProgress(float progress)
    {
        if (!cubeMovement) return;
        cubeMovement.SyncProgress(progress);
    }

    public void ApplyTemporarySpeedBoost(float extraSpeed, float distance)
    {
        if (!cubeMovement) return;
        cubeMovement.ApplyTemporarySpeedBoost(extraSpeed, distance);
    }

    public void LockYAxisTemporarily(float duration, float speedMultiplier, float speedMultiplierDuration)
    {
        if (!cubeMovement) return;
        cubeMovement.LockYAxisTemporarily(duration, speedMultiplier, speedMultiplierDuration);
    }

    public override void SetCustomTimeScale(float timeScale)
    {
        base.SetCustomTimeScale(timeScale);
        if (!cubeMovement) return;
        cubeMovement.SetCustomTimeScale(timeScale);
    }
}
