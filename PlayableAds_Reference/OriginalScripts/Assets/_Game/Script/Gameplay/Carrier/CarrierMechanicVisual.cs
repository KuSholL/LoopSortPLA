using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class CarrierMechanicVisual : MonoBehaviour
{
    public virtual void SetBeforeDisappearCallback(Action callback)
    {
    }

    public virtual void ApplyVisualRequest(CarrierVisualRequest request)
    {
    }

    public virtual UniTask PlayDisappearAnimationAsync()
    {
        return UniTask.CompletedTask;
    }
}
