using System.Collections.Generic;
using UnityEngine;

public class CustomTimeScaleGroup : MonoSingleton<CustomTimeScaleGroup>
{
    [SerializeField] private List<MonoBehaviour> targets = new List<MonoBehaviour>();

    public float CurrentTimeScale { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        CurrentTimeScale = 1f;
        ApplyTimeScale(1f);
    }

    public void AddTarget(MonoBehaviour target)
    {
        if (target == null || targets.Contains(target)) return;
        targets.Add(target);
        var scaleTarget = target as ICustomTimeScaleTarget;
        if (scaleTarget != null)
        {
            scaleTarget.SetCustomTimeScale(CurrentTimeScale);
        }
    }

    public void RemoveTarget(MonoBehaviour target)
    {
        if (target != null) targets.Remove(target);
    }

    public void ClearTargets()
    {
        targets.Clear();
    }

    public void ApplyTimeScale(float timeScale)
    {
        timeScale = Mathf.Max(0f, timeScale);
        if (!Mathf.Approximately(CurrentTimeScale, timeScale) &&
            GameEventBus.OnCustomTimeScaleChanged != null)
        {
            GameEventBus.OnCustomTimeScaleChanged(timeScale);
        }

        CurrentTimeScale = timeScale;
        for (var i = targets.Count - 1; i >= 0; i--)
        {
            var behaviour = targets[i];
            if (behaviour == null)
            {
                targets.RemoveAt(i);
                continue;
            }

            var target = behaviour as ICustomTimeScaleTarget;
            if (target != null)
            {
                target.SetCustomTimeScale(timeScale);
            }
        }
    }
}
