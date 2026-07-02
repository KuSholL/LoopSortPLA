using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public abstract class CubeBase : MonoBehaviour, ICustomTimeScaleTarget
{
    public Transform Trans;
    [SerializeField] private CubeDeliveryHandler cubeDeliveryHandler;
    [SerializeField] private CubeVisual cubeVisual;

    private void OnValidate()
    {
        Trans =  GetComponent<Transform>();
    }

    public void InitCube(EBlockColorType colorType)
    {
        cubeVisual.Setup(colorType);
    }

    public async UniTask FlyToTarget(Vector3 targetPos, Action onComplete, float? customHeight = null, float? customDuration = null)
    {
        await cubeDeliveryHandler.FlyToTarget(targetPos, onComplete, customHeight, customDuration);
    }

    public virtual void SetCustomTimeScale(float timeScale)
    {
        if (cubeDeliveryHandler == null) return;
        cubeDeliveryHandler.SetCustomTimeScale(timeScale);
    }

    protected virtual void OnDisable()
    {
        if (CustomTimeScaleGroup.Instance != null)
        {
            CustomTimeScaleGroup.Instance.RemoveTarget(this);
        }
    }
}
