using UnityEngine;

public class Cube : CubeBase
{
	[SerializeField]
	private CubeMovement cubeMovement;

	public void Setup(ConveyorPathRuntime splineContainer, float startProgress = 0f, float progressOffset = 0f, Vector3? initialWorldPosition = null)
	{
		cubeMovement.Setup(splineContainer, startProgress, progressOffset, initialWorldPosition);
	}

	public bool HasCompletedLap()
	{
		return (bool)cubeMovement && cubeMovement.HasCompletedLap();
	}

	public bool HasStartedFirstLoop()
	{
		return (bool)cubeMovement && cubeMovement.HasStartedFirstLoop();
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
		if ((bool)cubeMovement)
		{
			cubeMovement.SyncProgress(progress);
		}
	}

	public void ApplyTemporarySpeedBoost(float extraSpeed, float distance)
	{
		if ((bool)cubeMovement)
		{
			cubeMovement.ApplyTemporarySpeedBoost(extraSpeed, distance);
		}
	}

	public void LockYAxisTemporarily(float duration, float speedMultiplier, float speedMultiplierDuration)
	{
		if ((bool)cubeMovement)
		{
			cubeMovement.LockYAxisTemporarily(duration, speedMultiplier, speedMultiplierDuration);
		}
	}

	public override void SetCustomTimeScale(float timeScale)
	{
		base.SetCustomTimeScale(timeScale);
		if ((bool)cubeMovement)
		{
			cubeMovement.SetCustomTimeScale(timeScale);
		}
	}
}
