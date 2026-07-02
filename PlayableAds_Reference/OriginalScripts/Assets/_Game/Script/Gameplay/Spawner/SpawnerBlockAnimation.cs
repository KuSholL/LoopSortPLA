using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class SpawnerBlockAnimation : MonoBehaviour, ICustomTimeScaleTarget
{
    [Header("Flight Settings")]
    [SerializeField] private Transform startPoint;
    [SerializeField] private float staggerDelay = 0.05f;
    [SerializeField] private float flightHeight = 0.2f;
    [SerializeField] private float customDuration = 0.2f;

    private readonly List<AnimCube> _spawnedCubes = new();
    private bool _isAnimating;
    public bool IsAnimating => _isAnimating;
    private float _currentTimeScale = 1f;
    private System.Threading.CancellationTokenSource _animCts;

    public void SetCustomTimeScale(float timeScale)
    {
        _currentTimeScale = timeScale;
    }

    public float GetTotalDuration(int cubeCount)
    {
        if (cubeCount <= 0) return 0f;
        return (cubeCount - 1) * staggerDelay + customDuration;
    }

    public void Play(Block singleBlock, Action onComplete = null)
    {
        Cancel();
        _animCts = new System.Threading.CancellationTokenSource();
        PlayAsync(singleBlock, _animCts.Token, onComplete).Forget();
    }

    private async UniTaskVoid PlayAsync(Block singleBlock, System.Threading.CancellationToken token, Action onComplete)
    {
        if (singleBlock == null)
        {
            onComplete?.Invoke();
            return;
        }

        var colorType = singleBlock.GetBlockColorType();
        var cubeCount = singleBlock.GetCurrentCubes();

        if (colorType == EBlockColorType.None || cubeCount <= 0)
        {
            singleBlock.transform.localScale = Vector3.one;
            singleBlock.transform.localPosition = Vector3.zero;
            singleBlock.SetVisualCubes(cubeCount, suppressProgressAnimation: true);
            onComplete?.Invoke();
            return;
        }

        _isAnimating = true;

        singleBlock.transform.localPosition = Vector3.zero;
        singleBlock.transform.localScale = Vector3.zero;

        singleBlock.SetVisualCubes(0, suppressProgressAnimation: true);

        var animCubePrefab = ConfigManager.Instance != null ? ConfigManager.Instance.GetAnimCubePrefab() : null;
        if (animCubePrefab == null)
        {
            singleBlock.transform.localScale = Vector3.one;
            singleBlock.SetVisualCubes(cubeCount, suppressProgressAnimation: true);
            _isAnimating = false;
            onComplete?.Invoke();
            return;
        }

        Vector3 startPos = startPoint != null ? startPoint.position : transform.position;
        
        Vector3 basePos = singleBlock.transform.position;
        if (singleBlock.AnimationPivot != null)
        {
            basePos += singleBlock.transform.rotation * singleBlock.AnimationPivot.localPosition;
        }

        var flyTasks = new List<UniTask>();
        int arrivedCount = 0;

        for (int i = 0; i < cubeCount; i++)
        {
            if (token.IsCancellationRequested) return;

            var animCube = PoolManagerNew.Instance.PopFromPool(animCubePrefab, null);
            if (animCube != null)
            {
                animCube.transform.position = startPos;
                animCube.InitCube(colorType);
                
                _spawnedCubes.Add(animCube);
                if (CustomTimeScaleGroup.Instance != null)
                {
                    CustomTimeScaleGroup.Instance.AddTarget(animCube);
                }

                var flyTask = FlyCubeAsync(animCube, basePos, flightHeight, () =>
                {
                    if (arrivedCount == 0)
                    {
                        singleBlock.transform.localScale = Vector3.one;
                    }
                    arrivedCount++;
                    singleBlock.SetVisualCubes(arrivedCount, suppressProgressAnimation: false);
                    if (arrivedCount == cubeCount)
                    {
                        singleBlock.PlayMergeVfx();
                    }
                });
                flyTasks.Add(flyTask);
            }

            if (staggerDelay > 0f && i < cubeCount - 1)
            {
                await DelayWithCustomTimeScale(staggerDelay, token);
            }
        }

        if (token.IsCancellationRequested) return;

        await UniTask.WhenAll(flyTasks);

        if (token.IsCancellationRequested) return;

        ClearSpawnedCubes();

        _isAnimating = false;
        onComplete?.Invoke();
    }

    private async UniTask FlyCubeAsync(AnimCube cube, Vector3 targetPos, float height, Action onArrived)
    {
        if (cube == null) return;
        var tcs = new UniTaskCompletionSource();
        
        try
        {
            cube.FlyToTarget(targetPos, () =>
            {
                try
                {
                    onArrived?.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
                
                _spawnedCubes.Remove(cube);
                if (CustomTimeScaleGroup.Instance != null)
                {
                    CustomTimeScaleGroup.Instance.RemoveTarget(cube);
                }
                
                if (PoolManagerNew.Instance != null)
                {
                    PoolManagerNew.Instance.PushToPool(cube);
                }
                else
                {
                    Destroy(cube.gameObject);
                }
                tcs.TrySetResult();
            }, height, customDuration).Forget();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            tcs.TrySetResult();
        }
        
        await tcs.Task;
    }

    private async UniTask DelayWithCustomTimeScale(float durationSeconds, System.Threading.CancellationToken token)
    {
        float elapsed = 0f;
        while (elapsed < durationSeconds)
        {
            if (token.IsCancellationRequested) return;
            await UniTask.Yield();
            elapsed += Time.unscaledDeltaTime * _currentTimeScale;
        }
    }

    public void Cancel()
    {
        if (_animCts != null)
        {
            _animCts.Cancel();
            _animCts.Dispose();
            _animCts = null;
        }
        ClearSpawnedCubes();
        _isAnimating = false;
    }

    private void ClearSpawnedCubes()
    {
        foreach (var cube in _spawnedCubes)
        {
            if (cube != null)
            {
                if (CustomTimeScaleGroup.Instance != null)
                {
                    CustomTimeScaleGroup.Instance.RemoveTarget(cube);
                }
                if (PoolManagerNew.Instance != null)
                {
                    PoolManagerNew.Instance.PushToPool(cube);
                }
                else
                {
                    Destroy(cube.gameObject);
                }
            }
        }
        _spawnedCubes.Clear();
    }

    private void OnEnable()
    {
        GameEventBus.OnInitLoadLevel += Cancel;
        if (CustomTimeScaleGroup.Instance != null)
        {
            CustomTimeScaleGroup.Instance.AddTarget(this);
        }
    }

    private void OnDisable()
    {
        GameEventBus.OnInitLoadLevel -= Cancel;
        Cancel();
        if (CustomTimeScaleGroup.Instance != null)
        {
            CustomTimeScaleGroup.Instance.RemoveTarget(this);
        }
    }

    private void OnDestroy()
    {
        GameEventBus.OnInitLoadLevel -= Cancel;
        Cancel();
        if (CustomTimeScaleGroup.Instance != null)
        {
            CustomTimeScaleGroup.Instance.RemoveTarget(this);
        }
    }
}
