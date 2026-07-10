using System;
using UnityEngine;

public abstract class CubeBase : MonoBehaviour, ICustomTimeScaleTarget
{
	public Transform Trans;

	[SerializeField]
	private CubeDeliveryHandler cubeDeliveryHandler;

	[SerializeField]
	private CubeVisual cubeVisual;

	private void OnValidate()
	{
		Trans = GetComponent<Transform>();
	}

	public void InitCube(EBlockColorType colorType)
	{
		cubeVisual.Setup(colorType);
	}

	public void FlyToTarget(Vector3 targetPos, Action onComplete, float? customHeight = null, float? customDuration = null)
	{
		if (cubeDeliveryHandler == null)
		{
			onComplete?.Invoke();
		}
		else
		{
			cubeDeliveryHandler.FlyToTarget(targetPos, onComplete, customHeight, customDuration);
		}
	}

	public virtual void SetCustomTimeScale(float timeScale)
	{
		if (!(cubeDeliveryHandler == null))
		{
			cubeDeliveryHandler.SetCustomTimeScale(timeScale);
		}
	}

	protected virtual void OnDisable()
	{
		if (MonoSingleton<CustomTimeScaleGroup>.Instance != null)
		{
			MonoSingleton<CustomTimeScaleGroup>.Instance.RemoveTarget(this);
		}
	}
}
