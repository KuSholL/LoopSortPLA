using System;
using System.Collections.Generic;
using UnityEngine;

public class CustomTimeScaleGroup : MonoSingleton<CustomTimeScaleGroup>
{
    [SerializeField] private List<MonoBehaviour> targets = new();

    protected override void Awake()
    {
        base.Awake();
        ApplyTimeScale(1f);
    }

    private bool IsClawBoosterTransferring()
    {
        bool hasPopup = LayerManager.Instance != null && LayerManager.Instance.IsAnyPopupShowing();
        return !hasPopup 
            && BoosterSystem.Instance != null 
            && BoosterSystem.Instance.UseClawBooster 
            && BoosterSystem.Instance.IsClawAnimating;
    }

    public void AddTarget(MonoBehaviour target)
    {
        if (!target) return;
        if (!targets.Contains(target))
        {
            targets.Add(target);
        }
        
        if (target is ICustomTimeScaleTarget scaleTarget)
        {
            float targetTimeScale = CurrentTimeScale;
            if (IsClawBoosterTransferring() && (target is AnimCube || target is SpawnerBlockAnimation || target is SpawnerRemainingSlimeAnimator))
            {
                targetTimeScale = 1f;
            }
            scaleTarget.SetCustomTimeScale(targetTimeScale);
        }
    }

    public void RemoveTarget(MonoBehaviour target)
    {
        if (target == null) return;
        targets.Remove(target);
    }

    public void ClearTargets()
    {
        targets.Clear();
    }
    
    public float CurrentTimeScale { get; private set; } = 1f;
    
    public void ApplyTimeScale(float timeScale)
    {
        if (GameStateManager.Instance != null && GameStateManager.Instance.CurrentGameState == GameState.MainMenu)
        {
            timeScale = 0f;
        }

        timeScale = Mathf.Max(0f, timeScale);

        if (CurrentTimeScale != timeScale)
        {
            Debug.Log("Time Scale:"+ timeScale);
            GameEventBus.OnCustomTimeScaleChanged?.Invoke(timeScale);
        }
        
        CurrentTimeScale = timeScale;
        foreach (var t in targets)
        {
            if (t == null) continue;
            if (t is not ICustomTimeScaleTarget target) continue;
            
            float targetTimeScale = timeScale;
            if (IsClawBoosterTransferring() && (t is AnimCube || t is SpawnerBlockAnimation || t is SpawnerRemainingSlimeAnimator))
            {
                targetTimeScale = 1f;
            }
            target.SetCustomTimeScale(targetTimeScale);
        }
    }
}
