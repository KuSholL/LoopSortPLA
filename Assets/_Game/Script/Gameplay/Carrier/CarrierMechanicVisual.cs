using System;
using UnityEngine;

public class CarrierMechanicVisual : MonoBehaviour
{
    public virtual void SetBeforeDisappearCallback(Action callback)
    {
    }

    public virtual void ApplyVisualRequest(CarrierVisualRequest request)
    {
    }

    public virtual void PlayDisappearAnimation(Action onComplete)
    {
        onComplete?.Invoke();
    }
}
